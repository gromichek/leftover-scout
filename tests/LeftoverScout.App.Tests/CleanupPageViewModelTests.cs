using LeftoverScout.App.Services;
using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.Tests;

public class CleanupPageViewModelTests
{
    private const string Root = @"C:\Users\SampleUser";
    private static readonly ScanTarget Target = new(Root, isCurrentUser: true);

    private sealed class FakeGuard : ICleanupGuard
    {
        public Func<CleanupItem, GuardResult> ResultFor { get; set; } = _ => new GuardResult([], []);

        public Exception? Throw { get; set; }

        public int Calls { get; private set; }

        public CleanupGuardReport Check(CleanupRequest request)
        {
            Calls++;
            if (Throw is not null)
            {
                throw Throw;
            }

            RequestReduction reduction = RequestReducer.Reduce(request.Items);
            return new CleanupGuardReport(reduction, [.. reduction.Items.Select(item => new GuardItemResult(item, ResultFor(item)))]);
        }
    }

    private sealed class FakeDetector : IRunningProgramDetector
    {
        public Func<CleanupRequest, IReadOnlyList<RunningProgramWarning>> Run { get; set; } = _ => [];

        public CleanupRequest? LastRequest { get; private set; }

        public IReadOnlyList<RunningProgramWarning> Find(CleanupRequest request)
        {
            LastRequest = request;
            return Run(request);
        }
    }

    private sealed class FakeService : ICleanupService
    {
        public Func<CleanupRequest, CleanupMode, IProgress<CleanupProgress>?, CancellationToken, Task<CleanupSessionResult>>? Run { get; set; }

        public List<(CleanupRequest Request, CleanupMode Mode)> Calls { get; } = [];

        public Task<CleanupSessionResult> RunAsync(
            CleanupRequest request,
            CleanupMode mode,
            IProgress<CleanupProgress>? progress,
            CancellationToken cancellationToken,
            Action<CleanupItemResult>? itemCompleted = null)
        {
            Calls.Add((request, mode));
            return Run is null
                ? Task.FromResult(AllRemoved(request, mode))
                : Run(request, mode, progress, cancellationToken);
        }
    }

    private sealed class FakeDialogs : IDialogService
    {
        public Queue<bool> Answers { get; } = new();

        public List<(string Title, string Message)> Asked { get; } = [];

        public bool Confirm(string title, string message)
        {
            Asked.Add((title, message));
            return Answers.Count > 0 && Answers.Dequeue();
        }
    }

    private sealed class RecordingShell : IShellLauncher
    {
        public int RecycleBinOpened { get; private set; }

        public void OpenFolder(string folderPath)
        {
        }

        public void SelectFile(string filePath)
        {
        }

        public void OpenRecycleBin() => RecycleBinOpened++;

        public void OpenUrl(string url)
        {
        }
    }

    // Runs posted callbacks inline and stays the current context while they run, so continuations after an
    // await (and Progress<T> created in them) keep using this context instead of falling back to the thread pool.
    private sealed class ImmediateContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => Run(d, state);

        public override void Send(SendOrPostCallback d, object? state) => Run(d, state);

        private void Run(SendOrPostCallback d, object? state)
        {
            SynchronizationContext? previous = Current;
            SetSynchronizationContext(this);
            try
            {
                d(state);
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }
    }

    private sealed class Fixture
    {
        public FakeGuard Guard { get; } = new();

        public FakeDetector Detector { get; } = new();

        public FakeService Service { get; } = new();

        public FakeDialogs Dialogs { get; } = new();

        public CleanupState State { get; } = new();

        public RecordingShell Shell { get; } = new();

        public CleanupPageViewModel Page { get; }

        public Fixture()
        {
            Page = new CleanupPageViewModel(Guard, Detector, Service, Dialogs, State, Shell);
        }
    }

    private static CleanupItem Item(string name, LeftoverConfidence? confidence = LeftoverConfidence.High, long size = 1000, string? program = null) =>
        new(Root + @"\AppData\Roaming\" + name, confidence is null ? FindingKind.Cache : FindingKind.Leftover, confidence, size, null, program);

    private static CleanupRequest Request(params CleanupItem[] items) => new(Target, items);

    private static ScanResult Scan() => new(ScanCompletion.Completed, null, [], [], 0, 0, 0, TimeSpan.Zero);

    private static CleanupSessionResult AllRemoved(CleanupRequest request, CleanupMode mode) =>
        new(DateTime.UnixEpoch, DateTime.UnixEpoch, mode, [.. request.Items.Select(item => new CleanupItemResult(item, CleanupOutcome.Removed, item.SizeBytes))]);

    private static async Task<Fixture> Reviewed(params CleanupItem[] items)
    {
        var fixture = new Fixture();
        await fixture.Page.BeginReviewAsync(Request(items), Scan());
        return fixture;
    }

    [Fact]
    public void Constructor_StartsInTheNothingSelectedState()
    {
        var fixture = new Fixture();

        Assert.True(fixture.Page.IsNothingSelected);
        Assert.Equal("cleanup", fixture.Page.HelpTopicId);
        Assert.Equal("Cleanup", fixture.Page.Title);
        Assert.False(fixture.Page.StartCleanupCommand.CanExecute(null));
    }

    [Fact]
    public void GoToFindings_FromTheEmptyState_RaisesRequest()
    {
        var fixture = new Fixture();
        int raised = 0;
        fixture.Page.GoToFindingsRequested += (_, _) => raised++;

        fixture.Page.GoToFindingsCommand.Execute(null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task BeginReview_AllAllowed_ShowsReadyRowsAndTotals()
    {
        Fixture fixture = await Reviewed(Item("OldApp", size: 1000), Item("Cache", null, 500));

        Assert.True(fixture.Page.IsReview);
        Assert.Equal(2, fixture.Page.ReviewRows.Count);
        Assert.All(fixture.Page.ReviewRows, row => Assert.Equal("Ready", row.StatusText));
        Assert.Equal($"2 items · {SizeFormatter.Format(1500)}", fixture.Page.TotalsText);
        Assert.True(fixture.Page.StartCleanupCommand.CanExecute(null));
        Assert.Equal(CleanupMode.RecycleBin, fixture.Page.Mode);
        Assert.True(fixture.Page.IsRecycleBinMode);
        Assert.Equal("Space is freed when you empty the Recycle Bin.", fixture.Page.ModeNote);
    }

    [Fact]
    public async Task BeginReview_BlockedItems_AreExcludedAndShownWithReasons()
    {
        var fixture = new Fixture();
        fixture.Guard.ResultFor = item => item.FullPath.EndsWith("Gone", StringComparison.Ordinal)
            ? new GuardResult([BlockReason.Of(BlockReasonCode.NotFound)], [])
            : new GuardResult([], []);

        await fixture.Page.BeginReviewAsync(Request(Item("OldApp"), Item("Gone")), Scan());

        CleanupReviewRowViewModel blocked = fixture.Page.ReviewRows.Single(row => row.IsBlocked);
        Assert.StartsWith("Blocked: ", blocked.StatusText, StringComparison.Ordinal);
        Assert.Equal($"1 item · {SizeFormatter.Format(1000)}", fixture.Page.TotalsText);
    }

    [Fact]
    public async Task BeginReview_Warnings_MergedItemsAndRunningPrograms_AreShown()
    {
        var fixture = new Fixture();
        fixture.Guard.ResultFor = item => item.Confidence == LeftoverConfidence.Low
            ? new GuardResult([], [GuardWarning.Of(GuardWarningCode.LowConfidence)])
            : new GuardResult([], []);
        fixture.Detector.Run = request => [new RunningProgramWarning(request.Items[0], ["Discord"])];
        CleanupItem outer = Item("Outer");
        CleanupItem inner = Item(@"Outer\Cache", null, 10);
        CleanupItem low = Item("Fresh", LeftoverConfidence.Low);

        await fixture.Page.BeginReviewAsync(Request(outer, inner, low), Scan());

        Assert.Equal(2, fixture.Page.ReviewRows.Count);
        Assert.Contains(fixture.Page.ReviewRows, row => row.StatusText == "Ready · Low confidence");
        Assert.Equal("1 item is inside another selected item and is included with it.", fixture.Page.MergedLine);
        Assert.Equal("Close these programs first: Discord", fixture.Page.RunningWarning);
        Assert.True(fixture.Page.HasRunningWarning);
        Assert.True(fixture.Page.StartCleanupCommand.CanExecute(null));
    }

    [Fact]
    public async Task BeginReview_RunningProgramCheck_OnlySeesAllowedItems()
    {
        var fixture = new Fixture();
        fixture.Guard.ResultFor = item => item.FullPath.EndsWith("Gone", StringComparison.Ordinal)
            ? new GuardResult([BlockReason.Of(BlockReasonCode.NotFound)], [])
            : new GuardResult([], []);

        await fixture.Page.BeginReviewAsync(Request(Item("OldApp"), Item("Gone")), Scan());

        Assert.Equal([Item("OldApp").FullPath], fixture.Detector.LastRequest!.Items.Select(item => item.FullPath));
    }

    [Fact]
    public async Task BeginReview_NothingReady_DisablesStart()
    {
        var fixture = new Fixture();
        fixture.Guard.ResultFor = _ => new GuardResult([BlockReason.Of(BlockReasonCode.NotFound)], []);

        await fixture.Page.BeginReviewAsync(Request(Item("Gone")), Scan());

        Assert.True(fixture.Page.IsReview);
        Assert.False(fixture.Page.StartCleanupCommand.CanExecute(null));
        Assert.Equal("0 items · 0 B", fixture.Page.TotalsText);
    }

    [Fact]
    public async Task BeginReview_CheckFails_ShowsMessageAndDisablesStart()
    {
        var fixture = new Fixture();
        fixture.Guard.Throw = new InvalidOperationException("boom");

        await fixture.Page.BeginReviewAsync(Request(Item("OldApp")), Scan());

        Assert.True(fixture.Page.IsReview);
        Assert.True(fixture.Page.HasReviewMessage);
        Assert.False(fixture.Page.StartCleanupCommand.CanExecute(null));
        Assert.Empty(fixture.Service.Calls);
    }

    [Fact]
    public async Task Start_RecycleBin_AsksOnceAndPassesOnlyReadyItems()
    {
        var fixture = new Fixture();
        fixture.Guard.ResultFor = item => item.FullPath.EndsWith("Gone", StringComparison.Ordinal)
            ? new GuardResult([BlockReason.Of(BlockReasonCode.NotFound)], [])
            : new GuardResult([], []);
        await fixture.Page.BeginReviewAsync(Request(Item("OldApp"), Item("Gone")), Scan());
        fixture.Dialogs.Answers.Enqueue(true);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        (string title, string message) = Assert.Single(fixture.Dialogs.Asked);
        Assert.Equal("Confirm cleanup", title);
        Assert.Equal($"Move 1 item ({SizeFormatter.Format(1000)}) to the Recycle Bin?", message);
        (CleanupRequest request, CleanupMode mode) = Assert.Single(fixture.Service.Calls);
        Assert.Equal(CleanupMode.RecycleBin, mode);
        Assert.Equal([Item("OldApp").FullPath], request.Items.Select(item => item.FullPath));
        Assert.Equal(Root, request.Target.ProfileRootPath);
        Assert.True(fixture.Page.IsSummary);
    }

    [Fact]
    public async Task Start_RecycleBin_AnsweringNo_RunsNothing()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        fixture.Dialogs.Answers.Enqueue(false);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.Single(fixture.Dialogs.Asked);
        Assert.Empty(fixture.Service.Calls);
        Assert.True(fixture.Page.IsReview);
        Assert.False(fixture.State.IsRunning);
    }

    [Fact]
    public async Task Start_LowConfidenceItems_AreMentionedInTheQuestion()
    {
        Fixture fixture = await Reviewed(Item("Fresh", LeftoverConfidence.Low), Item("Other", LeftoverConfidence.Low));
        fixture.Dialogs.Answers.Enqueue(false);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.Contains("Including 2 items with Low confidence", fixture.Dialogs.Asked[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Start_Permanent_AsksTwiceAndThenRuns()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        fixture.Page.IsPermanentMode = true;
        fixture.Dialogs.Answers.Enqueue(true);
        fixture.Dialogs.Answers.Enqueue(true);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.Equal(2, fixture.Dialogs.Asked.Count);
        Assert.Equal($"Delete 1 item ({SizeFormatter.Format(1000)}) permanently?", fixture.Dialogs.Asked[0].Message);
        Assert.Equal($"This cannot be undone. Delete 1 item ({SizeFormatter.Format(1000)}) permanently?", fixture.Dialogs.Asked[1].Message);
        Assert.Equal(CleanupMode.Permanent, Assert.Single(fixture.Service.Calls).Mode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_Permanent_NoAtAnyStep_RunsNothing(bool firstAnswer)
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        fixture.Page.IsPermanentMode = true;
        fixture.Dialogs.Answers.Enqueue(firstAnswer);
        fixture.Dialogs.Answers.Enqueue(false);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.Empty(fixture.Service.Calls);
        Assert.True(fixture.Page.IsReview);
        Assert.Equal(firstAnswer ? 2 : 1, fixture.Dialogs.Asked.Count);
    }

    [Fact]
    public async Task ModeChoice_ChangesTheNoteAndIsResetByANewReview()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));

        fixture.Page.IsPermanentMode = true;
        Assert.Equal(CleanupMode.Permanent, fixture.Page.Mode);
        Assert.False(fixture.Page.IsRecycleBinMode);
        Assert.Contains("cannot be restored", fixture.Page.ModeNote, StringComparison.Ordinal);

        await fixture.Page.BeginReviewAsync(Request(Item("OldApp")), Scan());
        Assert.Equal(CleanupMode.RecycleBin, fixture.Page.Mode);
    }

    [Fact]
    public async Task Running_ShowsProgress_DisablesScan_AndCancelStopsTheService()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new ImmediateContext());
        try
        {
            Fixture fixture = await Reviewed(Item("OldApp", size: 1000), Item("Other", size: 2000));
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken seen = default;
            fixture.Service.Run = async (request, mode, progress, token) =>
            {
                seen = token;
                progress?.Report(new CleanupProgress(1, 2, @"AppData\Roaming\Other", 1000));
                await release.Task;
                return AllRemoved(request, mode);
            };
            fixture.Dialogs.Answers.Enqueue(true);
            var scanPage = new ScanPageViewModel(new IdleScanService(), fixture.State);
            Assert.True(scanPage.StartScanCommand.CanExecute(null));

            Task running = fixture.Page.StartCleanupCommand.ExecuteAsync(null);

            Assert.True(fixture.Page.IsRunning);
            Assert.True(fixture.State.IsRunning);
            Assert.False(scanPage.StartScanCommand.CanExecute(null));
            Assert.Equal(2, fixture.Page.ProgressMaximum);
            Assert.Equal(1, fixture.Page.ProgressValue);
            Assert.Equal(@"AppData\Roaming\Other", fixture.Page.CurrentItemText);
            Assert.Equal($"Item 2 of 2 · {SizeFormatter.Format(1000)} of {SizeFormatter.Format(3000)} removed", fixture.Page.ProgressText);
            Assert.True(fixture.Page.CancelCleanupCommand.CanExecute(null));

            fixture.Page.CancelCleanupCommand.Execute(null);

            Assert.True(seen.IsCancellationRequested);
            Assert.False(fixture.Page.CancelCleanupCommand.CanExecute(null));
            Assert.Equal("Stopping after the current item…", fixture.Page.RunningHint);

            release.SetResult(true);
            await running;

            Assert.True(fixture.Page.IsSummary);
            Assert.False(fixture.State.IsRunning);
            Assert.True(scanPage.StartScanCommand.CanExecute(null));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class IdleScanService : IScanService
    {
        public ScanResult? LastResult => null;

        public event EventHandler? LastResultChanged
        {
            add { }
            remove { }
        }

        public Task<ScanResult> RunAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task BeginReview_WhileRunning_IsIgnored()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Service.Run = async (request, mode, _, _) =>
        {
            await release.Task;
            return AllRemoved(request, mode);
        };
        fixture.Dialogs.Answers.Enqueue(true);
        Task running = fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        await fixture.Page.BeginReviewAsync(Request(Item("Another")), Scan());

        Assert.True(fixture.Page.IsRunning);
        release.SetResult(true);
        await running;
        Assert.Single(fixture.Page.ResultRows);
    }

    [Fact]
    public async Task Summary_AfterCleanup_MarksTheScanResultStale()
    {
        var fixture = new Fixture();
        ScanResult scan = Scan();
        await fixture.Page.BeginReviewAsync(Request(Item("OldApp")), scan);
        fixture.Dialogs.Answers.Enqueue(true);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.True(fixture.State.IsStale(scan));
        Assert.False(fixture.State.IsStale(Scan()));
    }

    [Fact]
    public async Task Summary_CancelledSession_ShowsCancelledAndMarksStale()
    {
        var fixture = new Fixture();
        ScanResult scan = Scan();
        CleanupItem first = Item("First");
        CleanupItem second = Item("Second");
        fixture.Service.Run = (_, mode, _, _) => Task.FromResult(new CleanupSessionResult(
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            mode,
            [new CleanupItemResult(first, CleanupOutcome.Removed, 1000), new CleanupItemResult(second, CleanupOutcome.NotStarted, Reason: "Cancelled.")]));
        await fixture.Page.BeginReviewAsync(Request(first, second), scan);
        fixture.Dialogs.Answers.Enqueue(true);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.Equal("Cleanup cancelled", fixture.Page.SummaryTitle);
        Assert.Contains("1 item not started, because the cleanup was cancelled.", fixture.Page.SummaryLines);
        Assert.True(fixture.State.IsStale(scan));
    }

    [Fact]
    public async Task Summary_ShowsCountsBytesRowsAndNotes()
    {
        var fixture = new Fixture();
        CleanupItem one = Item("One", size: 1000);
        CleanupItem two = Item("Two", size: 2000);
        fixture.Service.Run = (_, mode, _, _) => Task.FromResult(new CleanupSessionResult(
            DateTime.UnixEpoch,
            DateTime.UnixEpoch,
            mode,
            [
                new CleanupItemResult(one, CleanupOutcome.Removed, 1000, DeletedPermanentlyUnexpectedly: true),
                new CleanupItemResult(two, CleanupOutcome.SkippedTooLargeForRecycleBin, Reason: "Too large for the Recycle Bin."),
            ])
        {
            JournalWarning = "The journal could not be written completely.",
        });
        await fixture.Page.BeginReviewAsync(Request(one, two), Scan());
        fixture.Dialogs.Answers.Enqueue(true);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.Equal("Cleanup finished", fixture.Page.SummaryTitle);
        Assert.Equal(
            ["1 item moved to the Recycle Bin.", "1 item skipped: too large for the Recycle Bin."],
            fixture.Page.SummaryLines);
        Assert.Equal($"{SizeFormatter.Format(1000)} removed", fixture.Page.BytesRemovedText);
        Assert.True(fixture.Page.HasRecycleBinNote);
        Assert.Equal("The journal could not be written completely.", fixture.Page.JournalWarning);
        Assert.True(fixture.Page.HasUnexpectedPermanentWarning);
        Assert.Equal(["Removed", "Skipped (too large)"], fixture.Page.ResultRows.Select(row => row.OutcomeText));
        Assert.Equal(@"AppData\Roaming\One", fixture.Page.ResultRows[0].Path);
        Assert.Equal("Too large for the Recycle Bin.", fixture.Page.ResultRows[1].ReasonText);
    }

    [Fact]
    public async Task Summary_ServiceThrows_ShowsAnHonestMessageAndStillMarksStale()
    {
        var fixture = new Fixture();
        ScanResult scan = Scan();
        fixture.Service.Run = (_, _, _, _) => throw new InvalidOperationException("boom");
        await fixture.Page.BeginReviewAsync(Request(Item("OldApp")), scan);
        fixture.Dialogs.Answers.Enqueue(true);

        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);

        Assert.True(fixture.Page.IsSummary);
        Assert.Equal("The cleanup stopped unexpectedly", fixture.Page.SummaryTitle);
        Assert.True(fixture.State.IsStale(scan));
        Assert.False(fixture.State.IsRunning);
    }

    [Fact]
    public async Task SummaryButtons_RaiseRequestsAndOpenTheRecycleBin()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        fixture.Dialogs.Answers.Enqueue(true);
        await fixture.Page.StartCleanupCommand.ExecuteAsync(null);
        int history = 0;
        int scan = 0;
        fixture.Page.OpenHistoryRequested += (_, _) => history++;
        fixture.Page.ScanAgainRequested += (_, _) => scan++;

        fixture.Page.OpenHistoryCommand.Execute(null);
        fixture.Page.ScanAgainCommand.Execute(null);
        fixture.Page.OpenRecycleBinCommand.Execute(null);

        Assert.Equal(1, history);
        Assert.Equal(1, scan);
        Assert.Equal(1, fixture.Shell.RecycleBinOpened);
    }

    [Fact]
    public async Task BackToFindings_DropsTheReviewAndRaisesRequest()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        int raised = 0;
        fixture.Page.GoToFindingsRequested += (_, _) => raised++;

        fixture.Page.GoToFindingsCommand.Execute(null);

        Assert.Equal(1, raised);
        Assert.True(fixture.Page.IsNothingSelected);
        Assert.False(fixture.Page.StartCleanupCommand.CanExecute(null));
    }

    [Fact]
    public async Task CanCloseNow_NothingRunning_IsTrueWithoutAsking()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));

        Assert.True(fixture.Page.CanCloseNow());
        Assert.Empty(fixture.Dialogs.Asked);
    }

    [Fact]
    public async Task CanCloseNow_Running_AnsweringNo_KeepsRunning()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken seen = default;
        fixture.Service.Run = async (request, mode, _, token) =>
        {
            seen = token;
            await release.Task;
            return AllRemoved(request, mode);
        };
        fixture.Dialogs.Answers.Enqueue(true);
        Task running = fixture.Page.StartCleanupCommand.ExecuteAsync(null);
        fixture.Dialogs.Answers.Enqueue(false);

        bool canClose = fixture.Page.CanCloseNow();

        Assert.False(canClose);
        Assert.Equal("A cleanup is running. Stop after the current item and exit?", fixture.Dialogs.Asked[^1].Message);
        Assert.False(seen.IsCancellationRequested);
        release.SetResult(true);
        await running;
    }

    [Fact]
    public async Task CanCloseNow_Running_AnsweringYes_StopsAfterTheItemAndRaisesExitReady()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken seen = default;
        fixture.Service.Run = async (request, mode, _, token) =>
        {
            seen = token;
            await release.Task;
            return AllRemoved(request, mode);
        };
        fixture.Dialogs.Answers.Enqueue(true);
        Task running = fixture.Page.StartCleanupCommand.ExecuteAsync(null);
        fixture.Dialogs.Answers.Enqueue(true);
        int exitReady = 0;
        fixture.Page.ExitReady += (_, _) => exitReady++;

        bool canClose = fixture.Page.CanCloseNow();

        Assert.False(canClose);
        Assert.True(seen.IsCancellationRequested);
        Assert.Equal(0, exitReady);
        release.SetResult(true);
        await running;
        Assert.Equal(1, exitReady);
        Assert.True(fixture.Page.CanCloseNow());
    }

    [Fact]
    public async Task MainViewModel_ExitDuringCleanup_ClosesTheWindowAfterTheItem()
    {
        Fixture fixture = await Reviewed(Item("OldApp"));
        var main = new MainViewModel([fixture.Page], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Service.Run = async (request, mode, _, _) =>
        {
            await release.Task;
            return AllRemoved(request, mode);
        };
        fixture.Dialogs.Answers.Enqueue(true);
        Task running = fixture.Page.StartCleanupCommand.ExecuteAsync(null);
        fixture.Dialogs.Answers.Enqueue(true);
        int closeRequested = 0;
        main.CloseRequested += (_, _) => closeRequested++;

        Assert.False(main.CanCloseNow());
        Assert.Equal(0, closeRequested);
        release.SetResult(true);
        await running;

        Assert.Equal(1, closeRequested);
        Assert.True(main.CanCloseNow());
    }
}

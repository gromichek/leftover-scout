using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Core.Tests.Cleanup;

public class CleanupServiceTests
{
    private const string Root = FakeDirectoryReader.Root;

    private sealed class FakeGuard : ICleanupGuard
    {
        public HashSet<string> Blocked { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<int> RequestSizes { get; } = [];

        public CleanupGuardReport Check(CleanupRequest request)
        {
            RequestSizes.Add(request.Items.Count);
            var results = request.Items
                .Select(item => new GuardItemResult(
                    item,
                    Blocked.Contains(item.FullPath)
                        ? new GuardResult([BlockReason.Of(BlockReasonCode.NotFound)], [])
                        : new GuardResult([], [])))
                .ToList();
            return new CleanupGuardReport(new RequestReduction(request.Items, []), results);
        }
    }

    private sealed class FakeBin : IRecycleBinSettingsReader
    {
        public RecycleBinSettings Settings { get; set; } = RecycleBinSettings.Unknown;

        public int Reads { get; private set; }

        public RecycleBinSettings Read(string path)
        {
            Reads++;
            return Settings;
        }
    }

    private static CleanupItem Item(string name, long size = 100) =>
        new(Root + "\\" + name, FindingKind.Cache, null, size, null);

    private static CleanupRequest Request(params CleanupItem[] items) =>
        new(new ScanTarget(Root, isCurrentUser: false), items);

    private static CleanupService Service(FakeGuard guard, FakeFolderRemover remover, FakeBin bin, FakeDirectoryReader? reader = null) =>
        new(guard, remover, bin, reader ?? new FakeDirectoryReader(), TimeProvider.System, NullLogger<CleanupService>.Instance);

    [Fact]
    public async Task RunAsync_MixedResults_ReportsEveryOutcomeAndBytes()
    {
        var guard = new FakeGuard();
        var remover = new FakeFolderRemover();
        remover.Results[Root + @"\B"] = new FolderRemoveResult(FolderRemoveStatus.PartlyRemoved, 2, "2 item(s) could not be removed.");
        remover.Results[Root + @"\C"] = new FolderRemoveResult(FolderRemoveStatus.Failed, Reason: "in use");
        remover.Results[Root + @"\D"] = new FolderRemoveResult(FolderRemoveStatus.NotFound);
        remover.Results[Root + @"\E"] = new FolderRemoveResult(FolderRemoveStatus.NotRecycled, Reason: "vetoed");
        // B has 30 bytes left of 100.
        var reader = new FakeDirectoryReader().Folder(Root + @"\B", FakeDirectoryReader.FileEntry("locked.bin", 30));

        CleanupSessionResult result = await Service(guard, remover, new FakeBin(), reader).RunAsync(
            Request(Item("A"), Item("B"), Item("C"), Item("D"), Item("E")),
            CleanupMode.RecycleBin,
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [CleanupOutcome.Removed, CleanupOutcome.PartlyRemoved, CleanupOutcome.Failed, CleanupOutcome.NotFound, CleanupOutcome.NotRecycled],
            result.Items.Select(item => item.Outcome));
        Assert.Equal(100 + 70, result.BytesRemoved);
        Assert.Equal(2, result.Items[1].ItemsLeft);
        Assert.Equal(CleanupMode.RecycleBin, result.Mode);
        Assert.False(result.WasCancelled);
        Assert.False(result.DeletedPermanentlyUnexpectedly);
    }

    [Fact]
    public async Task RunAsync_GuardBlocksSecondItem_ItIsNotTouched()
    {
        var guard = new FakeGuard();
        guard.Blocked.Add(Root + @"\B");
        var remover = new FakeFolderRemover();

        CleanupSessionResult result = await Service(guard, remover, new FakeBin()).RunAsync(
            Request(Item("A"), Item("B"), Item("C")),
            CleanupMode.Permanent,
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal([Root + @"\A", Root + @"\C"], remover.Removed);
        CleanupItemResult blocked = result.Items[1];
        Assert.Equal(CleanupOutcome.Blocked, blocked.Outcome);
        Assert.Equal(BlockReasonCode.NotFound, Assert.Single(blocked.BlockReasons!).Code);
        Assert.Equal(0, blocked.BytesRemoved);
    }

    [Fact]
    public async Task RunAsync_GuardIsCheckedAgainBeforeEachItem()
    {
        var guard = new FakeGuard();

        await Service(guard, new FakeFolderRemover(), new FakeBin()).RunAsync(
            Request(Item("A"), Item("B"), Item("C")),
            CleanupMode.Permanent,
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal([1, 1, 1], guard.RequestSizes);
    }

    [Fact]
    public async Task RunAsync_CancelledDuringFirstItem_FirstCompletesRestNotStarted()
    {
        using var cts = new CancellationTokenSource();
        var remover = new FakeFolderRemover { OnRemove = _ => cts.Cancel() };

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A"), Item("B"), Item("C")),
            CleanupMode.Permanent,
            null,
            cts.Token);

        Assert.Equal([CleanupOutcome.Removed, CleanupOutcome.NotStarted, CleanupOutcome.NotStarted], result.Items.Select(item => item.Outcome));
        Assert.Equal([Root + @"\A"], remover.Removed);
        Assert.True(result.WasCancelled);
    }

    [Fact]
    public async Task RunAsync_AlreadyCancelled_NothingStarts()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var remover = new FakeFolderRemover();

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A")),
            CleanupMode.RecycleBin,
            null,
            cts.Token);

        Assert.Empty(remover.Removed);
        Assert.Equal(CleanupOutcome.NotStarted, Assert.Single(result.Items).Outcome);
    }

    [Fact]
    public async Task RunAsync_RecycleBinOff_SkipsWithoutDeleting()
    {
        var remover = new FakeFolderRemover();
        var bin = new FakeBin { Settings = new RecycleBinSettings(true, null) };

        CleanupSessionResult result = await Service(new FakeGuard(), remover, bin).RunAsync(
            Request(Item("A")), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken);

        Assert.Equal(CleanupOutcome.SkippedRecycleBinOff, Assert.Single(result.Items).Outcome);
        Assert.Empty(remover.Removed);
    }

    [Fact]
    public async Task RunAsync_ItemLargerThanBin_SkipsWithoutDeleting()
    {
        var remover = new FakeFolderRemover();
        var bin = new FakeBin { Settings = new RecycleBinSettings(false, 50) };

        CleanupSessionResult result = await Service(new FakeGuard(), remover, bin).RunAsync(
            Request(Item("A", 100), Item("B", 40)), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken);

        Assert.Equal([CleanupOutcome.SkippedTooLargeForRecycleBin, CleanupOutcome.Removed], result.Items.Select(item => item.Outcome));
        Assert.Equal([Root + @"\B"], remover.Removed);
    }

    [Fact]
    public async Task RunAsync_PermanentMode_DoesNotConsultTheRecycleBin()
    {
        var bin = new FakeBin { Settings = new RecycleBinSettings(true, 1) };

        CleanupSessionResult result = await Service(new FakeGuard(), new FakeFolderRemover(), bin).RunAsync(
            Request(Item("A", 100)), CleanupMode.Permanent, null, TestContext.Current.CancellationToken);

        Assert.Equal(CleanupOutcome.Removed, Assert.Single(result.Items).Outcome);
        Assert.Equal(0, bin.Reads);
    }

    [Fact]
    public async Task RunAsync_UnexpectedPermanentDeletion_IsFlagged()
    {
        var remover = new FakeFolderRemover();
        remover.Results[Root + @"\A"] = new FolderRemoveResult(FolderRemoveStatus.Removed, DeletedPermanentlyUnexpectedly: true);

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A")), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken);

        Assert.True(result.DeletedPermanentlyUnexpectedly);
        Assert.True(Assert.Single(result.Items).DeletedPermanentlyUnexpectedly);
        Assert.NotNull(result.Items[0].Reason);
    }

    [Fact]
    public async Task RunAsync_RemoverThrows_ItemFailsAndSessionContinues()
    {
        var remover = new FakeFolderRemover { OnRemove = path => { if (path.EndsWith('A')) { throw new InvalidOperationException("secret path"); } } };

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A"), Item("B")), CleanupMode.Permanent, null, TestContext.Current.CancellationToken);

        Assert.Equal([CleanupOutcome.Failed, CleanupOutcome.Removed], result.Items.Select(item => item.Outcome));
        Assert.DoesNotContain("secret", result.Items[0].Reason);
    }

    [Fact]
    public async Task RunAsync_ReportsProgressBeforeEachItemWithRelativePaths()
    {
        var reports = new List<CleanupProgress>();
        var progress = new SyncProgress(reports.Add);

        await Service(new FakeGuard(), new FakeFolderRemover(), new FakeBin()).RunAsync(
            Request(Item("A"), Item("B")), CleanupMode.Permanent, progress, TestContext.Current.CancellationToken);

        Assert.Equal(["A", "B"], reports.Select(report => report.CurrentRelativePath));
        Assert.Equal([0, 1], reports.Select(report => report.ItemIndex));
        Assert.Equal([0L, 100L], reports.Select(report => report.BytesDone));
        Assert.All(reports, report => Assert.Equal(2, report.ItemCount));
    }

    [Fact]
    public async Task RunAsync_NestedItems_AreReducedToTheOuterItem()
    {
        var remover = new FakeFolderRemover();

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A"), Item(@"A\Cache")), CleanupMode.Permanent, null, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal([Root + @"\A"], remover.Removed);
    }

    [Fact]
    public async Task RunAsync_ItemCompletedCallback_GetsEveryResultInOrder_IncludingNotStarted()
    {
        using var cts = new CancellationTokenSource();
        var remover = new FakeFolderRemover { OnRemove = _ => cts.Cancel() };
        var seen = new List<CleanupOutcome>();

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A"), Item("B")),
            CleanupMode.Permanent,
            null,
            cts.Token,
            item => seen.Add(item.Outcome));

        Assert.Equal(new[] { CleanupOutcome.Removed, CleanupOutcome.NotStarted }, seen);
        Assert.Equal(result.Items.Select(item => item.Outcome), seen);
    }

    [Fact]
    public async Task RunAsync_ItemCompletedCallbackThrows_CleanupGoesOn()
    {
        var remover = new FakeFolderRemover();

        CleanupSessionResult result = await Service(new FakeGuard(), remover, new FakeBin()).RunAsync(
            Request(Item("A"), Item("B")),
            CleanupMode.Permanent,
            null,
            TestContext.Current.CancellationToken,
            _ => throw new InvalidOperationException("Journal failure."));

        Assert.Equal(new[] { CleanupOutcome.Removed, CleanupOutcome.Removed }, result.Items.Select(item => item.Outcome));
        Assert.Equal([Root + @"\A", Root + @"\B"], remover.Removed);
    }

    private sealed class SyncProgress(Action<CleanupProgress> action) : IProgress<CleanupProgress>
    {
        public void Report(CleanupProgress value) => action(value);
    }
}

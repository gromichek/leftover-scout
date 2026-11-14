using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.App.Services;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Cleanup page (decision D-059): review the selected items, choose the mode, confirm, watch the progress and read
/// the summary. This is the only place that starts <see cref="ICleanupService.RunAsync"/>, and it passes only the
/// items that the safety guard allowed. The service checks every item again right before it touches it.
/// </summary>
public sealed partial class CleanupPageViewModel : PageViewModel
{
    private readonly ICleanupGuard _guard;
    private readonly IRunningProgramDetector _detector;
    private readonly ICleanupService _service;
    private readonly IDialogService _dialogs;
    private readonly CleanupState _state;
    private readonly IShellLauncher _shell;
    private CleanupRequest? _request;
    private ScanResult? _sourceResult;
    private CleanupReview? _review;
    private CancellationTokenSource? _cancellation;
    private int _checkVersion;
    private bool _exitRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNothingSelected))]
    [NotifyPropertyChangedFor(nameof(IsChecking))]
    [NotifyPropertyChangedFor(nameof(IsReview))]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(IsSummary))]
    [NotifyCanExecuteChangedFor(nameof(StartCleanupCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCleanupCommand))]
    private CleanupPageState _pageState = CleanupPageState.NothingSelected;

    [ObservableProperty]
    private IReadOnlyList<CleanupReviewRowViewModel> _reviewRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMergedLine))]
    private string _mergedLine = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRunningWarning))]
    private string _runningWarning = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReviewMessage))]
    private string _reviewMessage = string.Empty;

    [ObservableProperty]
    private string _totalsText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRecycleBinMode))]
    [NotifyPropertyChangedFor(nameof(IsPermanentMode))]
    [NotifyPropertyChangedFor(nameof(ModeNote))]
    private CleanupMode _mode = CleanupMode.RecycleBin;

    [ObservableProperty]
    private int _progressMaximum = 1;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _currentItemText = string.Empty;

    [ObservableProperty]
    private string _runningHint = "Cancel stops after the current item.";

    [ObservableProperty]
    private string _summaryTitle = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> _summaryLines = [];

    [ObservableProperty]
    private string _bytesRemovedText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecycleBinNote))]
    private bool _showRecycleBinNote;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasJournalWarning))]
    private string _journalWarning = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnexpectedPermanentWarning))]
    private string _unexpectedPermanentWarning = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<CleanupResultRowViewModel> _resultRows = [];

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Creates the Cleanup page.</summary>
    public CleanupPageViewModel(
        ICleanupGuard guard,
        IRunningProgramDetector detector,
        ICleanupService service,
        IDialogService dialogs,
        CleanupState state,
        IShellLauncher shell)
        : base("Cleanup", HelpTopics.Cleanup)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(shell);
        _guard = guard;
        _detector = detector;
        _service = service;
        _dialogs = dialogs;
        _state = state;
        _shell = shell;
    }

    /// <summary>Raised by "Go to Findings" and "Back to Findings"; the main window then switches to Findings.</summary>
    public event EventHandler? GoToFindingsRequested;

    /// <summary>Raised by "Open History"; the main window then switches to History.</summary>
    public event EventHandler? OpenHistoryRequested;

    /// <summary>Raised by "Scan again"; the main window then switches to Scan.</summary>
    public event EventHandler? ScanAgainRequested;

    /// <summary>Raised when the user asked to exit during a cleanup and the cleanup has ended; the window then closes.</summary>
    public event EventHandler? ExitReady;

    /// <summary>Text of the empty state.</summary>
    public string NothingSelectedMessage => "Nothing is selected for cleanup. Select the items you want to remove on the Findings page first.";

    /// <summary>Text shown while the checks run.</summary>
    public string CheckingMessage => "Checking…";

    /// <summary>Label of the Recycle Bin mode.</summary>
    public string RecycleBinModeLabel => "Move to Recycle Bin";

    /// <summary>Label of the permanent mode.</summary>
    public string PermanentModeLabel => "Delete permanently";

    /// <summary>True in the empty state.</summary>
    public bool IsNothingSelected => PageState == CleanupPageState.NothingSelected;

    /// <summary>True while the checks run.</summary>
    public bool IsChecking => PageState == CleanupPageState.Checking;

    /// <summary>True in the review state.</summary>
    public bool IsReview => PageState == CleanupPageState.Review;

    /// <summary>True while a cleanup runs.</summary>
    public bool IsRunning => PageState == CleanupPageState.Running;

    /// <summary>True when the summary is shown.</summary>
    public bool IsSummary => PageState == CleanupPageState.Summary;

    /// <summary>True when the merged-items line has text.</summary>
    public bool HasMergedLine => MergedLine.Length > 0;

    /// <summary>True when the running-program warning has text.</summary>
    public bool HasRunningWarning => RunningWarning.Length > 0;

    /// <summary>True when the review has a message (for example when the check failed).</summary>
    public bool HasReviewMessage => ReviewMessage.Length > 0;

    /// <summary>True when the Recycle Bin note is shown in the summary.</summary>
    public bool HasRecycleBinNote => ShowRecycleBinNote;

    /// <summary>The Recycle Bin note.</summary>
    public string RecycleBinNote => CleanupText.RecycleBinNote;

    /// <summary>True when the journal warning has text.</summary>
    public bool HasJournalWarning => JournalWarning.Length > 0;

    /// <summary>True when the unexpected-permanent-deletion warning has text.</summary>
    public bool HasUnexpectedPermanentWarning => UnexpectedPermanentWarning.Length > 0;

    /// <summary>Two-way binding of the Recycle Bin radio button.</summary>
    public bool IsRecycleBinMode
    {
        get => Mode == CleanupMode.RecycleBin;
        set
        {
            if (value)
            {
                Mode = CleanupMode.RecycleBin;
            }
        }
    }

    /// <summary>Two-way binding of the permanent radio button.</summary>
    public bool IsPermanentMode
    {
        get => Mode == CleanupMode.Permanent;
        set
        {
            if (value)
            {
                Mode = CleanupMode.Permanent;
            }
        }
    }

    /// <summary>The note under the mode choice.</summary>
    public string ModeNote => Mode == CleanupMode.RecycleBin
        ? CleanupText.RecycleBinNote
        : "Permanently deleted items cannot be restored. You will be asked twice.";

    /// <summary>
    /// Starts the review of a selection: runs the safety guard and the running-program check off the UI thread and shows
    /// the result. Ignored while a cleanup is running.
    /// </summary>
    /// <param name="request">The selected items.</param>
    /// <param name="sourceResult">The scan result they come from.</param>
    public async Task BeginReviewAsync(CleanupRequest request, ScanResult sourceResult)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sourceResult);

        if (IsRunning)
        {
            return;
        }

        int version = ++_checkVersion;
        _request = request;
        _sourceResult = sourceResult;
        _review = null;
        Mode = CleanupMode.RecycleBin;
        ReviewRows = [];
        MergedLine = string.Empty;
        RunningWarning = string.Empty;
        ReviewMessage = string.Empty;
        TotalsText = string.Empty;
        PageState = CleanupPageState.Checking;
        StatusMessage = "Checking the selected items…";

        CleanupReview? review = null;
        string message = string.Empty;
        try
        {
            review = await Task.Run(() => Check(request)).ConfigureAwait(true);
        }
        catch (Exception)
        {
            message = "The safety check could not be completed, so nothing can be cleaned. Nothing was changed.";
        }

        if (version != _checkVersion)
        {
            return;
        }

        _review = review;
        if (review is null)
        {
            ReviewRows = [];
        }
        else
        {
            ReviewRows = [.. review.Rows.Select(row => new CleanupReviewRowViewModel(row))];
        }

        MergedLine = review is null ? string.Empty : CleanupText.MergedLine(review.MergedCount);
        RunningWarning = review is { RunningPrograms.Count: > 0 }
            ? "Close these programs first: " + string.Join(", ", review.RunningPrograms)
            : string.Empty;
        ReviewMessage = message;
        TotalsText = CleanupText.Totals(review?.ReadyCount ?? 0, review?.ReadySize ?? 0);
        PageState = CleanupPageState.Review;
        StatusMessage = review is null
            ? "The safety check failed"
            : $"Review: {CleanupText.Totals(review.ReadyCount, review.ReadySize)} ready";
    }

    /// <summary>
    /// Asks whether the window may close now. During a cleanup it asks the user; on Yes it stops the cleanup after the
    /// current item and <see cref="ExitReady"/> is raised when the cleanup has ended. Returns true when nothing runs.
    /// </summary>
    public bool CanCloseNow()
    {
        if (!IsRunning)
        {
            return true;
        }

        if (!_exitRequested && _dialogs.Confirm("LeftoverScout", CleanupText.ExitQuestion))
        {
            _exitRequested = true;
            StopAfterCurrentItem();
        }

        return false;
    }

    private bool CanStart() => IsReview && _review is { ReadyCount: > 0 };

    private bool CanCancel() => IsRunning && _cancellation is { IsCancellationRequested: false };

    private CleanupReview Check(CleanupRequest request)
    {
        CleanupGuardReport report = _guard.Check(request);
        List<CleanupItem> allowed = [.. report.Items.Where(entry => entry.Result.IsAllowed).Select(entry => entry.Item)];
        IReadOnlyList<RunningProgramWarning> running = [];
        if (allowed.Count > 0)
        {
            running = _detector.Find(new CleanupRequest(request.Target, allowed));
        }

        return CleanupReviewBuilder.Build(request, report, running);
    }

    [RelayCommand]
    private void GoToFindings()
    {
        if (!IsRunning)
        {
            // Back to Findings: the review is dropped, a new selection starts a new review.
            _checkVersion++;
            _request = null;
            _sourceResult = null;
            _review = null;
            PageState = CleanupPageState.NothingSelected;
        }

        GoToFindingsRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartCleanupAsync()
    {
        if (_request is null || _review is not { ReadyCount: > 0 } review)
        {
            return;
        }

        CleanupMode mode = Mode;
        IReadOnlyList<CleanupItem> ready = review.ReadyItems;
        long readySize = review.ReadySize;

        // Confirmation: one question for the Recycle Bin, two for permanent deletion. No at any step runs nothing.
        string firstQuestion = CleanupText.FirstQuestion(mode, ready.Count, readySize, review.ReadyLowConfidenceCount);
        if (!_dialogs.Confirm("Confirm cleanup", firstQuestion))
        {
            return;
        }

        if (mode == CleanupMode.Permanent && !_dialogs.Confirm("Delete permanently", CleanupText.SecondQuestion(ready.Count, readySize)))
        {
            return;
        }

        var request = new CleanupRequest(_request.Target, ready);
        ScanResult? sourceResult = _sourceResult;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;

        ProgressMaximum = Math.Max(ready.Count, 1);
        ProgressValue = 0;
        ProgressText = $"Item 0 of {ready.Count:N0} · {SizeFormatter.Format(0)} of {SizeFormatter.Format(readySize)} removed";
        CurrentItemText = string.Empty;
        RunningHint = "Cancel stops after the current item.";
        PageState = CleanupPageState.Running;
        _state.IsRunning = true;
        StatusMessage = "Cleaning up…";

        // Created on the UI thread, so reports arrive on the UI thread.
        var progress = new Progress<CleanupProgress>(report =>
        {
            if (!IsRunning)
            {
                return;
            }

            ProgressValue = report.ItemIndex;
            ProgressMaximum = Math.Max(report.ItemCount, 1);
            CurrentItemText = report.CurrentRelativePath;
            ProgressText =
                $"Item {report.ItemIndex + 1:N0} of {report.ItemCount:N0} · " +
                $"{SizeFormatter.Format(report.BytesDone)} of {SizeFormatter.Format(readySize)} removed";
        });

        CleanupSessionResult? session = null;
        try
        {
            session = await _service.RunAsync(request, mode, progress, cancellation.Token).ConfigureAwait(true);
        }
        catch (Exception)
        {
            // The journal (History) records what happened; the page only says that the run ended unexpectedly.
        }
        finally
        {
            _cancellation = null;

            // The scan result no longer matches the disk (decision D-068), even when the cleanup stopped early.
            _state.MarkStale(sourceResult);
            ShowSummary(request, session);
            _state.IsRunning = false;
        }

        if (_exitRequested)
        {
            ExitReady?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelCleanup() => StopAfterCurrentItem();

    private void StopAfterCurrentItem()
    {
        if (_cancellation is { IsCancellationRequested: false } cancellation)
        {
            cancellation.Cancel();
            RunningHint = "Stopping after the current item…";
            StatusMessage = "Stopping after the current item…";
            CancelCleanupCommand.NotifyCanExecuteChanged();
        }
    }

    private void ShowSummary(CleanupRequest request, CleanupSessionResult? session)
    {
        if (session is null)
        {
            SummaryTitle = "The cleanup stopped unexpectedly";
            SummaryLines = ["Some items may already have been removed. Open History to see what was recorded."];
            BytesRemovedText = string.Empty;
            ShowRecycleBinNote = false;
            JournalWarning = string.Empty;
            UnexpectedPermanentWarning = string.Empty;
            ResultRows = [];
            StatusMessage = "Cleanup stopped unexpectedly";
            PageState = CleanupPageState.Summary;
            return;
        }

        string root = request.Target.ProfileRootPath;
        SummaryTitle = session.WasCancelled ? "Cleanup cancelled" : "Cleanup finished";
        SummaryLines = CleanupText.SummaryLines(session);
        BytesRemovedText = $"{SizeFormatter.Format(session.BytesRemoved)} removed";
        ShowRecycleBinNote = session.Mode == CleanupMode.RecycleBin && session.Items.Any(item => item.BytesRemoved > 0 || item.Outcome is CleanupOutcome.Removed or CleanupOutcome.PartlyRemoved);
        JournalWarning = session.JournalWarning ?? string.Empty;
        UnexpectedPermanentWarning = session.DeletedPermanentlyUnexpectedly ? CleanupText.UnexpectedPermanentWarning : string.Empty;
        ResultRows = [.. session.Items.Select(item => new CleanupResultRowViewModel(item, CleanupPaths.Relative(root, item.Item.FullPath)))];
        StatusMessage = $"{SummaryTitle}: {BytesRemovedText}";
        PageState = CleanupPageState.Summary;
    }

    [RelayCommand]
    private void OpenHistory() => OpenHistoryRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenRecycleBin() => _shell.OpenRecycleBin();

    [RelayCommand]
    private void ScanAgain() => ScanAgainRequested?.Invoke(this, EventArgs.Empty);
}

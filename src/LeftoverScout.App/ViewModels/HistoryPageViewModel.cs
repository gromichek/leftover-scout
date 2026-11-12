using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Journal;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The History page (decision D-060): the recorded cleanup sessions, newest first, and the details of the selected
/// one. Read-only; the actions open the Recycle Bin and the journal folder or copy a path. It never changes a journal.
/// </summary>
public sealed partial class HistoryPageViewModel : PageViewModel
{
    private readonly IJournalReader _reader;
    private readonly IJournalStorage _storage;
    private readonly IShellLauncher _shell;
    private readonly IClipboardService _clipboard;
    private readonly SynchronizationContext? _uiContext;
    private int _refreshVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoSessions))]
    [NotifyPropertyChangedFor(nameof(HasSessions))]
    private IReadOnlyList<HistorySessionRowViewModel> _sessions = [];

    [ObservableProperty]
    private HistorySessionRowViewModel? _selectedSession;

    [ObservableProperty]
    private IReadOnlyList<HistoryItemRowViewModel> _details = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CopyPathCommand))]
    private HistoryItemRowViewModel? _selectedDetail;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDetailsMessage))]
    private string _detailsMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnexpectedPermanentWarning))]
    private string _unexpectedPermanentWarning = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Creates the History page and starts loading the journals.</summary>
    /// <remarks>Create it on the UI thread: it refreshes through the captured synchronization context.</remarks>
    public HistoryPageViewModel(
        IJournalReader reader,
        ICleanupJournal journal,
        IJournalStorage storage,
        IShellLauncher shell,
        IClipboardService clipboard)
        : base("History", HelpTopics.History)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(clipboard);
        _reader = reader;
        _storage = storage;
        _shell = shell;
        _clipboard = clipboard;
        _uiContext = SynchronizationContext.Current;

        journal.SessionRecorded += OnSessionRecorded;
        _ = RefreshAsync();
    }

    /// <summary>Title of the empty state.</summary>
    public string EmptyTitle => "No cleanups yet.";

    /// <summary>One sentence under the empty-state title.</summary>
    public string EmptyDescription => "Every cleanup you run is recorded here: what was selected and what happened to each folder.";

    /// <summary>The journal folder, shown as a tooltip hint.</summary>
    public string JournalFolderPath => _storage.FolderPath;

    /// <summary>True when there are no journals.</summary>
    public bool HasNoSessions => Sessions.Count == 0;

    /// <summary>True when there is at least one journal.</summary>
    public bool HasSessions => Sessions.Count > 0;

    /// <summary>True when a note about the selected journal is shown (unreadable or interrupted).</summary>
    public bool HasDetailsMessage => DetailsMessage.Length > 0;

    /// <summary>True when an item of the selected session was deleted permanently although the Recycle Bin was asked for.</summary>
    public bool HasUnexpectedPermanentWarning => UnexpectedPermanentWarning.Length > 0;

    /// <summary>Reads the journals on a background thread and shows them. Safe to call at any time; never throws.</summary>
    public async Task RefreshAsync()
    {
        int version = Interlocked.Increment(ref _refreshVersion);
        IReadOnlyList<JournalEntry> entries;
        try
        {
            entries = await Task.Run(() => _reader.ReadAll()).ConfigureAwait(true);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // The reader does not throw for bad files; anything else leaves the list as it is.
            StatusMessage = $"The journals could not be read ({exception.GetType().Name}).";
            return;
        }

        // A newer refresh started meanwhile: its result is the one to show.
        if (version != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        string? selectedFile = SelectedSession?.FileName;
        List<HistorySessionRowViewModel> rows = entries.Select(entry => new HistorySessionRowViewModel(entry)).ToList();
        Sessions = rows;
        SelectedSession = rows.FirstOrDefault(row => row.FileName == selectedFile) ?? rows.FirstOrDefault();
    }

    partial void OnSelectedSessionChanged(HistorySessionRowViewModel? value) => ShowDetails(value);

    private bool HasSelectedDetail() => SelectedDetail is not null;

    [RelayCommand]
    private void OpenRecycleBin() => _shell.OpenRecycleBin();

    [RelayCommand]
    private void OpenJournalFolder()
    {
        // The folder is created on demand so that the button also works before the first cleanup.
        if (_storage.EnsureFolder())
        {
            _shell.OpenFolder(_storage.FolderPath);
        }
        else
        {
            StatusMessage = "The journal folder could not be opened.";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedDetail))]
    private void CopyPath()
    {
        if (SelectedDetail is { } detail)
        {
            StatusMessage = _clipboard.TrySetText(detail.FullPath)
                ? "Path copied to the clipboard"
                : "The path could not be copied because the clipboard is busy";
        }
    }

    private void OnSessionRecorded(object? sender, EventArgs e)
    {
        // The cleanup raises the event on a worker thread; the page state belongs to the UI thread.
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            _ = RefreshAsync();
        }
        else
        {
            _uiContext.Post(state => _ = RefreshAsync(), null);
        }
    }

    private void ShowDetails(HistorySessionRowViewModel? session)
    {
        SelectedDetail = null;
        JournalEntry? entry = session?.Entry;
        JournalDocument? document = entry?.Document;

        if (entry is null || document is null)
        {
            Details = [];
            UnexpectedPermanentWarning = string.Empty;
            DetailsMessage = entry is null
                ? string.Empty
                : $"The journal file {entry.FileName} could not be read. It was not changed.";
            return;
        }

        Details = document.Items.Select(item => new HistoryItemRowViewModel(item)).ToList();
        UnexpectedPermanentWarning = document.Items.Any(item => item.DeletedPermanentlyUnexpectedly)
            ? "Warning: Windows deleted at least one folder permanently instead of moving it to the Recycle Bin. It cannot be restored."
            : string.Empty;
        DetailsMessage = entry.Status switch
        {
            JournalEntryStatus.Interrupted =>
                "This cleanup was interrupted (the program closed or the computer stopped). Folders marked Pending were not recorded: check them in Explorer.",
            JournalEntryStatus.InProgress => "This cleanup is still running.",
            _ => string.Empty,
        };
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Findings page: leftovers and cache / temp / logs / crash dump folders of the last completed scan with filters,
/// a table and a details panel (decisions D-052 … D-055). Read-only; the actions are "Open in Explorer", "Copy path"
/// and "Show in Results". Filter state is kept while the app runs.
/// </summary>
public sealed partial class FindingsPageViewModel : PageViewModel
{
    private readonly IScanService _scanService;
    private readonly IShellLauncher _shell;
    private readonly IClipboardService _clipboard;
    private readonly SynchronizationContext? _uiContext;
    private ScanResult? _shownResult;
    private IReadOnlyList<Finding> _findings = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResult))]
    private bool _hasResult;

    [ObservableProperty]
    private string _headerText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showHigh = FindingFilter.Default.ShowHigh;

    [ObservableProperty]
    private bool _showMedium = FindingFilter.Default.ShowMedium;

    [ObservableProperty]
    private bool _showLow = FindingFilter.Default.ShowLow;

    [ObservableProperty]
    private bool _showCache = FindingFilter.Default.ShowCache;

    [ObservableProperty]
    private bool _showTemp = FindingFilter.Default.ShowTemp;

    [ObservableProperty]
    private bool _showLogs = FindingFilter.Default.ShowLogs;

    [ObservableProperty]
    private bool _showCrashDumps = FindingFilter.Default.ShowCrashDumps;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<FindingRowViewModel> _rows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(HasNoSelection))]
    [NotifyCanExecuteChangedFor(nameof(OpenInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyPathCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowInResultsCommand))]
    private FindingRowViewModel? _selectedRow;

    [ObservableProperty]
    private string _totalText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmptyMessage))]
    private string _emptyMessage = string.Empty;

    /// <summary>Creates the Findings page and shows the findings of the last result if there is one.</summary>
    /// <remarks>Create it on the UI thread: it refreshes through the captured synchronization context.</remarks>
    public FindingsPageViewModel(IScanService scanService, IShellLauncher shell, IClipboardService clipboard)
        : base("Findings", HelpTopics.Findings)
    {
        _scanService = scanService;
        _shell = shell;
        _clipboard = clipboard;
        _uiContext = SynchronizationContext.Current;

        _scanService.LastResultChanged += OnLastResultChanged;
        Refresh();
    }

    /// <summary>Raised when the user presses "Go to Scan"; the main window then switches to the Scan page.</summary>
    public event EventHandler? GoToScanRequested;

    /// <summary>Raised by "Show in Results" with the folder to show; the main window opens the Results page.</summary>
    public event EventHandler<FolderNode>? ShowInResultsRequested;

    /// <summary>Sort member of the table when it is first shown (the Size column).</summary>
    public const string DefaultSortMember = "Size";

    /// <summary>Sort direction of the table when it is first shown: largest first.</summary>
    public const System.ComponentModel.ListSortDirection DefaultSortDirection = System.ComponentModel.ListSortDirection.Descending;

    /// <summary>Message shown before the first completed scan.</summary>
    public string NoResultMessage => "There are no findings yet. Run a scan first, then come back here to see what you could remove.";

    /// <summary>Text of the empty details panel.</summary>
    public string NoSelectionMessage => "Select an item to see why it is listed.";

    /// <summary>True when there is nothing to show yet.</summary>
    public bool HasNoResult => !HasResult;

    /// <summary>True when a row is selected.</summary>
    public bool HasSelection => SelectedRow is not null;

    /// <summary>True when no row is selected.</summary>
    public bool HasNoSelection => SelectedRow is null;

    /// <summary>True when the empty-state message is shown. It sits over the grid and never takes mouse clicks.</summary>
    public bool HasEmptyMessage => EmptyMessage.Length > 0;

    partial void OnShowHighChanged(bool value) => ApplyFilter();

    partial void OnShowMediumChanged(bool value) => ApplyFilter();

    partial void OnShowLowChanged(bool value) => ApplyFilter();

    partial void OnShowCacheChanged(bool value) => ApplyFilter();

    partial void OnShowTempChanged(bool value) => ApplyFilter();

    partial void OnShowLogsChanged(bool value) => ApplyFilter();

    partial void OnShowCrashDumpsChanged(bool value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private bool HasSelectedRow() => SelectedRow is not null;

    [RelayCommand]
    private void GoToScan() => GoToScanRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void OpenInExplorer()
    {
        if (SelectedRow is { } row)
        {
            _shell.OpenFolder(row.FullPath);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void CopyPath()
    {
        if (SelectedRow is { } row)
        {
            StatusMessage = _clipboard.TrySetText(row.FullPath)
                ? "Path copied to the clipboard"
                : "The path could not be copied because the clipboard is busy";
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void ShowInResults()
    {
        if (SelectedRow is { } row)
        {
            ShowInResultsRequested?.Invoke(this, row.Finding.Node);
        }
    }

    private void OnLastResultChanged(object? sender, EventArgs e)
    {
        // The scan service raises the event on a background thread; the page state belongs to the UI thread.
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            Refresh();
        }
        else
        {
            _uiContext.Post(_ => Refresh(), null);
        }
    }

    private void Refresh()
    {
        ScanResult? result = _scanService.LastResult;

        // A cancelled scan keeps the previous result (decision D-039).
        if (ReferenceEquals(result, _shownResult))
        {
            return;
        }

        _shownResult = result;

        if (result?.Root is null)
        {
            _findings = [];
            HasResult = false;
            HeaderText = string.Empty;
            Rows = [];
            SelectedRow = null;
            TotalText = string.Empty;
            EmptyMessage = string.Empty;
            return;
        }

        _findings = FindingBuilder.Build(result);
        HeaderText = result.CompletedAt is { } completed ? $"Scanned {ResultsFormat.LocalDateTime(completed)}" : "Scanned";
        HasResult = true;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (_shownResult is null)
        {
            return;
        }

        var filter = new FindingFilter(ShowHigh, ShowMedium, ShowLow, ShowCache, ShowTemp, ShowLogs, ShowCrashDumps, SearchText);
        FindingList list = filter.Apply(_findings);
        SelectedRow = null;
        Rows = list.Entries
            .OrderByDescending(entry => entry.Finding.LocalSize)
            .ThenBy(entry => entry.Finding.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new FindingRowViewModel(entry))
            .ToList();
        TotalText = $"{list.Count:N0} {(list.Count == 1 ? "item" : "items")} · {SizeFormatter.Format(list.TotalSize)}";
        EmptyMessage = list.Count > 0
            ? string.Empty
            : _findings.Count == 0
                ? "Nothing was found that you could remove."
                : "No findings match the current filters.";
    }
}

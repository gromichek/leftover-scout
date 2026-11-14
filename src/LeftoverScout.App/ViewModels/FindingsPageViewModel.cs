using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Findings page: leftovers and cache / temp / logs / crash dump folders of the last completed scan with filters,
/// a table and a details panel (decisions D-052 … D-055). The actions are "Open in Explorer", "Copy path" and
/// "Show in Results", plus the selection for cleanup (D-056, D-069) and "Clean up selected…", which hands the selection
/// to the Cleanup page. This page never deletes anything. Filter state is kept while the app runs.
/// </summary>
public sealed partial class FindingsPageViewModel : PageViewModel
{
    private readonly IScanService _scanService;
    private readonly IShellLauncher _shell;
    private readonly IClipboardService _clipboard;
    private readonly CleanupState _cleanupState;
    private readonly SynchronizationContext? _uiContext;
    private readonly Dictionary<string, Finding> _selected = new(StringComparer.OrdinalIgnoreCase);
    private bool _changingSelection;
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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CleanUpSelectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearSelectionCommand))]
    private int _selectedCount;

    [ObservableProperty]
    private string _selectionText = CleanupText.SelectionLine(0, 0);

    /// <summary>Creates the Findings page and shows the findings of the last result if there is one.</summary>
    /// <remarks>Create it on the UI thread: it refreshes through the captured synchronization context.</remarks>
    /// <param name="scanService">Provides the last completed scan.</param>
    /// <param name="shell">Opens folders in Explorer.</param>
    /// <param name="clipboard">Copies paths.</param>
    /// <param name="cleanupState">Shared cleanup state (stale result, running cleanup); a private one when null.</param>
    public FindingsPageViewModel(IScanService scanService, IShellLauncher shell, IClipboardService clipboard, CleanupState? cleanupState = null)
        : base("Findings", HelpTopics.Findings)
    {
        _scanService = scanService;
        _shell = shell;
        _clipboard = clipboard;
        _cleanupState = cleanupState ?? new CleanupState();
        _uiContext = SynchronizationContext.Current;

        _scanService.LastResultChanged += OnLastResultChanged;
        _cleanupState.Changed += OnCleanupStateChanged;
        Refresh();
    }

    /// <summary>Raised when the user presses "Go to Scan"; the main window then switches to the Scan page.</summary>
    public event EventHandler? GoToScanRequested;

    /// <summary>Raised by "Show in Results" with the folder to show; the main window opens the Results page.</summary>
    public event EventHandler<FolderNode>? ShowInResultsRequested;

    /// <summary>Raised by "Clean up selected…"; the main window then opens the Cleanup page in the review state.</summary>
    public event EventHandler<CleanupRequestedEventArgs>? CleanupRequested;

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

    /// <summary>True when a cleanup made the shown result out of date (decision D-068).</summary>
    public bool IsResultStale => _cleanupState.IsStale(_shownResult);

    /// <summary>The banner text shown when the result is out of date.</summary>
    public string StaleMessage => CleanupText.StaleResultMessage;

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

    private bool CanCleanUpSelected() => SelectedCount > 0 && !IsResultStale && !_cleanupState.IsRunning;

    private bool HasChecked() => SelectedCount > 0;

    [RelayCommand]
    private void SelectAllVisible() => SetAllRows(true);

    [RelayCommand(CanExecute = nameof(HasChecked))]
    private void ClearSelection() => SetAllRows(false);

    /// <summary>Toggles the checkbox of the selected row (the Space key).</summary>
    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void ToggleSelectedRow()
    {
        if (SelectedRow is { } row)
        {
            row.IsSelected = !row.IsSelected;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCleanUpSelected))]
    private void CleanUpSelected()
    {
        if (_shownResult is not { Root: { } root } result || _selected.Count == 0 || !CanCleanUpSelected())
        {
            return;
        }

        // Phase 1 scans the current user's profile only (decision D-003); the root of the scan is the profile folder.
        var target = new ScanTarget(root.FullPath, isCurrentUser: true);
        CleanupRequest request = CleanupRequestBuilder.FromFindings(target, _selected.Values);
        CleanupRequested?.Invoke(this, new CleanupRequestedEventArgs(request, result));
    }

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
        _selected.Clear();
        OnPropertyChanged(nameof(IsResultStale));
        CleanUpSelectedCommand.NotifyCanExecuteChanged();

        if (result?.Root is null)
        {
            _findings = [];
            UpdateSelectionLine();
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

        // Items that become hidden are deselected, so nothing hidden can be cleaned by accident (decision D-069).
        var visiblePaths = new HashSet<string>(list.Entries.Select(entry => entry.Finding.FullPath), StringComparer.OrdinalIgnoreCase);
        foreach (string hidden in _selected.Keys.Where(path => !visiblePaths.Contains(path)).ToList())
        {
            _selected.Remove(hidden);
        }

        List<FindingRowViewModel> rows = list.Entries
            .OrderByDescending(entry => entry.Finding.LocalSize)
            .ThenBy(entry => entry.Finding.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new FindingRowViewModel(entry) { IsSelected = _selected.ContainsKey(entry.Finding.FullPath) })
            .ToList();
        foreach (FindingRowViewModel row in rows)
        {
            row.PropertyChanged += OnRowPropertyChanged;
        }

        Rows = rows;
        UpdateSelectionLine();
        TotalText = $"{list.Count:N0} {(list.Count == 1 ? "item" : "items")} · {SizeFormatter.Format(list.TotalSize)}";
        EmptyMessage = list.Count > 0
            ? string.Empty
            : _findings.Count == 0
                ? "Nothing was found that you could remove."
                : "No findings match the current filters.";
    }

    private void OnRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_changingSelection || e.PropertyName != nameof(FindingRowViewModel.IsSelected) || sender is not FindingRowViewModel row)
        {
            return;
        }

        if (row.IsSelected)
        {
            _selected[row.FullPath] = row.Finding;
        }
        else
        {
            _selected.Remove(row.FullPath);
        }

        UpdateSelectionLine();
    }

    // Sets every visible checkbox; the selection line is worked out once at the end.
    private void SetAllRows(bool selected)
    {
        _changingSelection = true;
        try
        {
            foreach (FindingRowViewModel row in Rows)
            {
                row.IsSelected = selected;
                if (selected)
                {
                    _selected[row.FullPath] = row.Finding;
                }
                else
                {
                    _selected.Remove(row.FullPath);
                }
            }
        }
        finally
        {
            _changingSelection = false;
        }

        UpdateSelectionLine();
    }

    private void UpdateSelectionLine()
    {
        SelectedCount = _selected.Count;
        SelectionText = CleanupText.SelectionLine(_selected.Count, CleanupRequestBuilder.SelectedSize(_selected.Values));
        CleanUpSelectedCommand.NotifyCanExecuteChanged();
    }

    private void OnCleanupStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsResultStale));
        CleanUpSelectedCommand.NotifyCanExecuteChanged();
    }
}

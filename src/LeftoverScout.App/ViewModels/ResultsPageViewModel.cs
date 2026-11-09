using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Results;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Results page: browse the folders of the last completed scan one folder at a time (decision D-031)
/// and list the large files (D-033). Everything here is read-only; the only actions are
/// "Open in Explorer" and "Copy path" (D-034).
/// </summary>
public sealed partial class ResultsPageViewModel : PageViewModel
{
    private readonly IScanService _scanService;
    private readonly IShellLauncher _shell;
    private readonly IClipboardService _clipboard;
    private readonly SynchronizationContext? _uiContext;
    private ScanResult? _shownResult;
    private FolderNavigator? _navigator;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResult))]
    private bool _hasResult;

    [ObservableProperty]
    private string _headerText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<BreadcrumbItem> _breadcrumb = [];

    [ObservableProperty]
    private IReadOnlyList<FolderRowViewModel> _rows = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenFolderInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyFolderPathCommand))]
    private FolderRowViewModel? _selectedRow;

    [ObservableProperty]
    private string _folderSummaryText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLargeFiles))]
    [NotifyPropertyChangedFor(nameof(HasNoLargeFiles))]
    private IReadOnlyList<LargeFileRowViewModel> _largeFiles = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenFileInExplorerCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyFilePathCommand))]
    private LargeFileRowViewModel? _selectedLargeFile;

    /// <summary>Creates the Results page and shows the last result if there is one.</summary>
    /// <remarks>
    /// Create it on the UI thread: the scan service may raise <see cref="IScanService.LastResultChanged"/>
    /// on a background thread, and the page refreshes through the captured synchronization context.
    /// </remarks>
    public ResultsPageViewModel(IScanService scanService, IShellLauncher shell, IClipboardService clipboard)
        : base("Results", HelpTopics.Results)
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

    /// <summary>Sort member of the folder table when it is first shown (the Size column).</summary>
    public const string DefaultSortMember = "Size";

    /// <summary>Sort direction of the folder table when it is first shown: largest first.</summary>
    public const System.ComponentModel.ListSortDirection DefaultSortDirection = System.ComponentModel.ListSortDirection.Descending;

    /// <summary>Message shown before the first completed scan.</summary>
    public string NoResultMessage => "There is no scan result yet. Run a scan first, then come back here to see where the space goes.";

    /// <summary>Message of the empty Large files tab.</summary>
    public string NoLargeFilesMessage => "No files of 100 MB or more.";

    /// <summary>True when there is no result to show.</summary>
    public bool HasNoResult => !HasResult;

    /// <summary>True when the scan found large files.</summary>
    public bool HasLargeFiles => LargeFiles.Count > 0;

    /// <summary>True when the scan found no large files.</summary>
    public bool HasNoLargeFiles => LargeFiles.Count == 0;

    private bool CanGoUp() => _navigator?.CanGoUp ?? false;

    private bool HasSelectedRow() => SelectedRow is not null;

    private bool HasSelectedLargeFile() => SelectedLargeFile is not null;

    [RelayCommand]
    private void GoToScan() => GoToScanRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanGoUp))]
    private void Up()
    {
        if (_navigator?.GoUp() == true)
        {
            ShowCurrentFolder();
        }
    }

    [RelayCommand]
    private void NavigateTo(BreadcrumbItem? item)
    {
        if (item is not null && _navigator?.NavigateTo(item.Node) == true)
        {
            ShowCurrentFolder();
        }
    }

    /// <summary>Opens a folder row (double-click or Enter). The parameter is the row; null uses the selected row.</summary>
    [RelayCommand]
    private void OpenFolder(FolderRowViewModel? row)
    {
        row ??= SelectedRow;
        if (row is not null && _navigator?.TryOpen(row.Row) == true)
        {
            ShowCurrentFolder();
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void OpenFolderInExplorer()
    {
        if (SelectedRow is { } row)
        {
            _shell.OpenFolder(row.FullPath);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void CopyFolderPath()
    {
        if (SelectedRow is { } row)
        {
            CopyPath(row.FullPath);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLargeFile))]
    private void OpenFileInExplorer()
    {
        if (SelectedLargeFile is { } file)
        {
            _shell.SelectFile(file.FullPath);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLargeFile))]
    private void CopyFilePath()
    {
        if (SelectedLargeFile is { } file)
        {
            CopyPath(file.FullPath);
        }
    }

    /// <summary>
    /// Opens the parent folder of <paramref name="node"/> and selects its row ("Show in Results", decision D-055).
    /// Returns false when the folder does not belong to the result that is shown.
    /// </summary>
    public bool ShowFolder(FolderNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (_navigator is not { } navigator || !navigator.GoTo(node.Parent ?? node))
        {
            return false;
        }

        ShowCurrentFolder();
        SelectedRow = Rows.FirstOrDefault(row => ReferenceEquals(row.Row.Node, node));
        return true;
    }

    private void CopyPath(string path) =>
        StatusMessage = _clipboard.TrySetText(path) ? "Path copied to the clipboard" : "The path could not be copied because the clipboard is busy";

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

        // A cancelled scan keeps the previous result: do not reset the folder the user is looking at.
        if (ReferenceEquals(result, _shownResult))
        {
            return;
        }

        _shownResult = result;

        if (result?.Root is not { } root)
        {
            _navigator = null;
            HasResult = false;
            HeaderText = string.Empty;
            Breadcrumb = [];
            Rows = [];
            LargeFiles = [];
            SelectedRow = null;
            SelectedLargeFile = null;
            FolderSummaryText = string.Empty;
            UpCommand.NotifyCanExecuteChanged();
            return;
        }

        _navigator = new FolderNavigator(root);
        HeaderText = BuildHeader(result, root);
        SelectedLargeFile = null;
        LargeFiles = LargeFileRowBuilder.Build(result.LargeFiles, root.FullPath)
            .Select(row => new LargeFileRowViewModel(row))
            .ToList();
        HasResult = true;
        ShowCurrentFolder();
    }

    private static string BuildHeader(ScanResult result, FolderNode root)
    {
        string when = result.CompletedAt is { } completed ? $"Scanned {ResultsFormat.LocalDateTime(completed)}" : "Scanned";
        return $"{when} · {SizeFormatter.Format(root.LocalSize)} local · {SizeFormatter.Format(root.CloudOnlySize)} cloud-only";
    }

    private void ShowCurrentFolder()
    {
        if (_navigator is not { } navigator)
        {
            return;
        }

        FolderNode current = navigator.Current;
        SelectedRow = null;
        Breadcrumb = navigator.Breadcrumb;
        Rows = FolderRowBuilder.Build(current, _shownResult?.RuleMatches, _shownResult?.Leftovers).Select(row => new FolderRowViewModel(row)).ToList();

        int folderCount = current.Children.Count;
        FolderSummaryText = $"{folderCount:N0} {(folderCount == 1 ? "folder" : "folders")} · {SizeFormatter.Format(current.LocalSize)} in this folder";
        UpCommand.NotifyCanExecuteChanged();
    }
}

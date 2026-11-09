using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The Programs page: the installed programs collected together with the last completed scan (decision D-037).
/// Read-only; the only actions are "Open install location" and "Copy name".
/// </summary>
public sealed partial class ProgramsPageViewModel : PageViewModel
{
    private readonly IScanService _scanService;
    private readonly IShellLauncher _shell;
    private readonly IClipboardService _clipboard;
    private readonly IFolderExistence _folders;
    private readonly SynchronizationContext? _uiContext;
    private EvidenceSnapshot? _shownSnapshot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResult))]
    private bool _hasResult;

    [ObservableProperty]
    private string _headerText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrors))]
    private string _errorText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _countText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEmptyMessage))]
    private string _emptyMessage = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private SourceFilterOption _selectedSource = SourceFilterOption.All[0];

    [ObservableProperty]
    private bool _showSystemComponents;

    [ObservableProperty]
    private IReadOnlyList<ProgramRowViewModel> _rows = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInstallLocationCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyNameCommand))]
    private ProgramRowViewModel? _selectedRow;

    /// <summary>Creates the Programs page and shows the evidence of the last result if there is one.</summary>
    /// <remarks>Create it on the UI thread: it refreshes through the captured synchronization context.</remarks>
    public ProgramsPageViewModel(IScanService scanService, IShellLauncher shell, IClipboardService clipboard, IFolderExistence folders)
        : base("Programs", HelpTopics.Programs)
    {
        _scanService = scanService;
        _shell = shell;
        _clipboard = clipboard;
        _folders = folders;
        _uiContext = SynchronizationContext.Current;

        _scanService.LastResultChanged += OnLastResultChanged;
        Refresh();
    }

    /// <summary>Raised when the user presses "Go to Scan"; the main window then switches to the Scan page.</summary>
    public event EventHandler? GoToScanRequested;

    /// <summary>Message shown before the first completed scan.</summary>
    public string NoResultMessage => "There is no list of programs yet. Run a scan first; it also collects the installed programs.";

    /// <summary>The choices of the Source filter.</summary>
    public IReadOnlyList<SourceFilterOption> SourceOptions => SourceFilterOption.All;

    /// <summary>True when there is nothing to show yet.</summary>
    public bool HasNoResult => !HasResult;

    /// <summary>
    /// True when the empty-state message is shown. The message sits over the grid, so the view collapses it when there is
    /// nothing to say and never lets it take mouse clicks.
    /// </summary>
    public bool HasEmptyMessage => EmptyMessage.Length > 0;

    /// <summary>True when at least one source could not be read.</summary>
    public bool HasErrors => ErrorText.Length > 0;

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedSourceChanged(SourceFilterOption value) => ApplyFilters();

    partial void OnShowSystemComponentsChanged(bool value) => ApplyFilters();

    private bool CanOpenInstallLocation() =>
        SelectedRow is { InstallLocation.Length: > 0 } row && _folders.FolderExists(row.InstallLocation);

    private bool HasSelectedRow() => SelectedRow is not null;

    [RelayCommand]
    private void GoToScan() => GoToScanRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand(CanExecute = nameof(CanOpenInstallLocation))]
    private void OpenInstallLocation()
    {
        if (SelectedRow is { InstallLocation.Length: > 0 } row)
        {
            _shell.OpenFolder(row.InstallLocation);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedRow))]
    private void CopyName()
    {
        if (SelectedRow is { } row)
        {
            StatusMessage = _clipboard.TrySetText(row.Name)
                ? "Name copied to the clipboard"
                : "The name could not be copied because the clipboard is busy";
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
        EvidenceSnapshot? snapshot = _scanService.LastResult?.Evidence;

        // A cancelled scan keeps the previous result and its evidence (decision D-039).
        if (ReferenceEquals(snapshot, _shownSnapshot))
        {
            return;
        }

        _shownSnapshot = snapshot;

        if (snapshot is null)
        {
            HasResult = false;
            HeaderText = string.Empty;
            ErrorText = string.Empty;
            CountText = string.Empty;
            EmptyMessage = string.Empty;
            Rows = [];
            SelectedRow = null;
            return;
        }

        int total = snapshot.Programs.Count;
        HeaderText = $"Collected {ResultsFormat.LocalDateTime(snapshot.CollectedAt)} · {total:N0} {(total == 1 ? "program" : "programs")}";
        ErrorText = string.Join(" ", snapshot.Errors.Select(error => error.Message));
        HasResult = true;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        if (_shownSnapshot is not { } snapshot)
        {
            return;
        }

        IReadOnlyList<InstalledProgram> shown = ProgramFilter.Apply(snapshot.Programs, SearchText, SelectedSource.Source, ShowSystemComponents);
        SelectedRow = null;
        Rows = shown.Select(program => new ProgramRowViewModel(program)).ToList();
        int hidden = ShowSystemComponents
            ? 0
            : ProgramFilter.Apply(snapshot.Programs, SearchText, SelectedSource.Source, includeSystemComponents: true).Count - shown.Count;
        CountText = hidden > 0
            ? $"{shown.Count:N0} of {snapshot.Programs.Count:N0} programs · {hidden:N0} hidden (system components and frameworks)"
            : $"{shown.Count:N0} of {snapshot.Programs.Count:N0} programs";
        EmptyMessage = shown.Count > 0
            ? string.Empty
            : snapshot.Programs.Count == 0
                ? "No installed programs were found."
                : "No programs match the current filters.";
    }
}

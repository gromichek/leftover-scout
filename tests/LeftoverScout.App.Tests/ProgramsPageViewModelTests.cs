using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.Tests;

public class ProgramsPageViewModelTests
{
    private sealed class StubScanService : IScanService
    {
        public ScanResult? LastResult { get; set; }

        public event EventHandler? LastResultChanged;

        public Task<ScanResult> RunAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Raise() => LastResultChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RecordingShell : IShellLauncher
    {
        public List<string> OpenedFolders { get; } = [];

        public void OpenFolder(string folderPath) => OpenedFolders.Add(folderPath);

        public void SelectFile(string filePath)
        {
        }

        public void OpenUrl(string url)
        {
        }
    }

    private sealed class FakeClipboard : IClipboardService
    {
        public bool Succeeds { get; set; } = true;

        public List<string> Texts { get; } = [];

        public bool TrySetText(string text)
        {
            Texts.Add(text);
            return Succeeds;
        }
    }

    private sealed class FakeFolders(params string[] existing) : IFolderExistence
    {
        public bool FolderExists(string folderPath) => existing.Contains(folderPath, StringComparer.OrdinalIgnoreCase);
    }

    private const string AppFolder = @"C:\Program Files\Sample Editor";

    private static EvidenceSnapshot Snapshot(IReadOnlyList<InstalledProgram>? programs = null, params EvidenceError[] errors) =>
        new(
            programs ?? DefaultPrograms,
            new Dictionary<ProgramSource, int>(),
            errors,
            new DateTimeOffset(2025, 3, 4, 12, 30, 0, TimeSpan.Zero),
            TimeSpan.FromMilliseconds(200));

    private static readonly InstalledProgram[] DefaultPrograms =
    [
        new("Sample Editor", ProgramSource.Registry, "k1", Publisher: "Sample Publisher", Version: "2.0", InstallLocation: AppFolder, InstallDate: new DateOnly(2024, 3, 15)),
        new("Sample Photos", ProgramSource.Store, "k2", Publisher: "Other Company"),
        new("Sample Runtime", ProgramSource.Registry, "k3", IsSystemComponent: true),
        new("Sample Framework", ProgramSource.Store, "k4", IsFramework: true),
        new("Sample Editor", ProgramSource.StartMenu, "Sample Editor.lnk"),
    ];

    private static ScanResult CompletedResult(EvidenceSnapshot? evidence) =>
        new(ScanCompletion.Completed, null, [], [], 0, 0, 0, TimeSpan.Zero) { CompletedAt = DateTimeOffset.UnixEpoch, Evidence = evidence };

    private static (ProgramsPageViewModel ViewModel, StubScanService Service, RecordingShell Shell, FakeClipboard Clipboard) Create(
        EvidenceSnapshot? snapshot = null,
        params string[] existingFolders)
    {
        var service = new StubScanService { LastResult = snapshot is null ? null : CompletedResult(snapshot) };
        var shell = new RecordingShell();
        var clipboard = new FakeClipboard();
        return (new ProgramsPageViewModel(service, shell, clipboard, new FakeFolders(existingFolders)), service, shell, clipboard);
    }

    [Fact]
    public void Constructor_NoScanYet_ShowsNoResultState()
    {
        var (viewModel, _, _, _) = Create();

        Assert.False(viewModel.HasResult);
        Assert.True(viewModel.HasNoResult);
        Assert.Empty(viewModel.Rows);
        Assert.Equal("programs", viewModel.HelpTopicId);
        Assert.Equal("Programs", viewModel.Title);
    }

    [Fact]
    public void Constructor_ResultWithoutEvidence_ShowsNoResultState()
    {
        var service = new StubScanService { LastResult = CompletedResult(null) };

        var viewModel = new ProgramsPageViewModel(service, new RecordingShell(), new FakeClipboard(), new FakeFolders());

        Assert.True(viewModel.HasNoResult);
    }

    [Fact]
    public void GoToScan_RaisesEvent()
    {
        var (viewModel, _, _, _) = Create();
        int raised = 0;
        viewModel.GoToScanRequested += (_, _) => raised++;

        viewModel.GoToScanCommand.Execute(null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Constructor_WithResult_HidesSystemComponentsAndShowsCounts()
    {
        var (viewModel, _, _, _) = Create(Snapshot());

        Assert.True(viewModel.HasResult);
        Assert.Equal(["Sample Editor", "Sample Editor", "Sample Photos"], viewModel.Rows.Select(r => r.Name));
        Assert.Equal("3 of 5 programs · 2 hidden (system components and frameworks)", viewModel.CountText);
        Assert.Contains("5 programs", viewModel.HeaderText, StringComparison.Ordinal);
        Assert.StartsWith("Collected ", viewModel.HeaderText, StringComparison.Ordinal);
        Assert.False(viewModel.HasErrors);
        Assert.Equal(string.Empty, viewModel.EmptyMessage);
        Assert.False(viewModel.HasEmptyMessage);
    }

    [Fact]
    public void Header_SingleProgram_UsesSingular()
    {
        var (viewModel, _, _, _) = Create(Snapshot([DefaultPrograms[0]]));

        Assert.EndsWith("1 program", viewModel.HeaderText, StringComparison.Ordinal);
    }

    [Fact]
    public void CountText_NothingHidden_UsesShortForm()
    {
        var (viewModel, _, _, _) = Create(Snapshot([new InstalledProgram("Sample Editor", ProgramSource.Registry, "k1")]));

        Assert.Equal("1 of 1 programs", viewModel.CountText);
    }

    [Fact]
    public void CountText_SearchNarrowsHiddenCount()
    {
        var (viewModel, _, _, _) = Create(Snapshot());

        viewModel.SearchText = "Runtime";

        Assert.Equal("0 of 5 programs · 1 hidden (system components and frameworks)", viewModel.CountText);
    }

    [Fact]
    public void ShowSystemComponents_Toggled_ShowsAllPrograms()
    {
        var (viewModel, _, _, _) = Create(Snapshot());

        viewModel.ShowSystemComponents = true;

        Assert.Equal(5, viewModel.Rows.Count);
        Assert.Equal("5 of 5 programs", viewModel.CountText);

        viewModel.ShowSystemComponents = false;

        Assert.Equal(3, viewModel.Rows.Count);
    }

    [Fact]
    public void SearchText_FiltersNameAndPublisherIgnoringCase()
    {
        var (viewModel, _, _, _) = Create(Snapshot());

        viewModel.SearchText = "PHOTOS";
        Assert.Equal(["Sample Photos"], viewModel.Rows.Select(r => r.Name));

        viewModel.SearchText = "sample publisher";
        Assert.Equal(["Sample Editor"], viewModel.Rows.Select(r => r.Name).Distinct());

        viewModel.SearchText = string.Empty;
        Assert.Equal(3, viewModel.Rows.Count);
    }

    [Fact]
    public void SelectedSource_Filters_AndOptionsListAllSources()
    {
        var (viewModel, _, _, _) = Create(Snapshot());
        Assert.Equal(["All", "Registry", "Store", "Program folder", "Start menu"], viewModel.SourceOptions.Select(o => o.Label));

        viewModel.SelectedSource = viewModel.SourceOptions.Single(o => o.Source == ProgramSource.Store);

        Assert.Equal(["Sample Photos"], viewModel.Rows.Select(r => r.Name));
        Assert.Equal("1 of 5 programs · 1 hidden (system components and frameworks)", viewModel.CountText);
    }

    [Fact]
    public void Filters_NothingMatches_ShowsEmptyMessage()
    {
        var (viewModel, _, _, _) = Create(Snapshot());

        viewModel.SearchText = "no such program";

        Assert.Empty(viewModel.Rows);
        Assert.Equal("0 of 5 programs", viewModel.CountText);
        Assert.Equal("No programs match the current filters.", viewModel.EmptyMessage);
        Assert.True(viewModel.HasEmptyMessage);
    }

    [Fact]
    public void EmptyMessage_ClearedAgain_IsNoLongerShown()
    {
        // The message sits over the grid: when there is nothing to say it must be collapsed so it cannot take mouse clicks.
        var (viewModel, _, _, _) = Create(Snapshot());
        viewModel.SearchText = "no such program";
        Assert.True(viewModel.HasEmptyMessage);

        viewModel.SearchText = string.Empty;

        Assert.False(viewModel.HasEmptyMessage);
        Assert.Equal(string.Empty, viewModel.EmptyMessage);
    }

    [Fact]
    public void Snapshot_WithoutPrograms_ShowsNoProgramsMessage()
    {
        var (viewModel, _, _, _) = Create(Snapshot([]));

        Assert.True(viewModel.HasResult);
        Assert.Equal("No installed programs were found.", viewModel.EmptyMessage);
        Assert.Equal("0 of 0 programs", viewModel.CountText);
    }

    [Fact]
    public void Errors_AreShownAsShortLines()
    {
        var (viewModel, _, _, _) = Create(Snapshot(null, new EvidenceError(ProgramSource.Store, "Store apps could not be read.")));

        Assert.True(viewModel.HasErrors);
        Assert.Equal("Store apps could not be read.", viewModel.ErrorText);
    }

    [Fact]
    public void Rows_ExposeTextsAndSortValues()
    {
        var (viewModel, _, _, _) = Create(Snapshot());

        ProgramRowViewModel row = viewModel.Rows.First(r => r.SourceText == "Registry");

        Assert.Equal("Sample Publisher", row.Publisher);
        Assert.Equal("2.0", row.Version);
        Assert.Equal(AppFolder, row.InstallLocation);
        Assert.Equal(new DateTime(2024, 3, 15), row.Installed);
        Assert.NotEmpty(row.InstalledText);

        ProgramRowViewModel photos = viewModel.Rows.Single(r => r.Name == "Sample Photos");
        Assert.Equal(string.Empty, photos.Version);
        Assert.Equal(string.Empty, photos.InstallLocation);
        Assert.Null(photos.Installed);
        Assert.Equal(string.Empty, photos.InstalledText);
    }

    [Fact]
    public void OpenInstallLocation_FolderExists_OpensIt()
    {
        var (viewModel, _, shell, _) = Create(Snapshot(), AppFolder);
        viewModel.SelectedRow = viewModel.Rows.First(r => r.SourceText == "Registry");

        Assert.True(viewModel.OpenInstallLocationCommand.CanExecute(null));
        viewModel.OpenInstallLocationCommand.Execute(null);

        Assert.Equal([AppFolder], shell.OpenedFolders);
    }

    [Fact]
    public void OpenInstallLocation_FolderMissingOrUnknown_IsDisabled()
    {
        var (viewModel, _, shell, _) = Create(Snapshot());

        Assert.False(viewModel.OpenInstallLocationCommand.CanExecute(null));

        viewModel.SelectedRow = viewModel.Rows.First(r => r.SourceText == "Registry");
        Assert.False(viewModel.OpenInstallLocationCommand.CanExecute(null));

        viewModel.SelectedRow = viewModel.Rows.Single(r => r.Name == "Sample Photos");
        Assert.False(viewModel.OpenInstallLocationCommand.CanExecute(null));
        Assert.Empty(shell.OpenedFolders);
    }

    [Fact]
    public void CopyName_PutsNameOnClipboardAndReportsInStatus()
    {
        var (viewModel, _, _, clipboard) = Create(Snapshot());
        Assert.False(viewModel.CopyNameCommand.CanExecute(null));
        viewModel.SelectedRow = viewModel.Rows.Single(r => r.Name == "Sample Photos");

        viewModel.CopyNameCommand.Execute(null);

        Assert.Equal(["Sample Photos"], clipboard.Texts);
        Assert.Equal("Name copied to the clipboard", viewModel.StatusMessage);
    }

    [Fact]
    public void CopyName_ClipboardBusy_ReportsFailure()
    {
        var (viewModel, _, _, clipboard) = Create(Snapshot());
        clipboard.Succeeds = false;
        viewModel.SelectedRow = viewModel.Rows.First();

        viewModel.CopyNameCommand.Execute(null);

        Assert.Contains("clipboard is busy", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void LastResultChanged_NewEvidence_ReplacesRows()
    {
        var (viewModel, service, _, _) = Create();
        service.LastResult = CompletedResult(Snapshot());

        service.Raise();

        Assert.True(viewModel.HasResult);
        Assert.Equal(3, viewModel.Rows.Count);
    }

    [Fact]
    public void LastResultChanged_CancelledScanKeepsPreviousEvidence_AndFilters()
    {
        var (viewModel, service, _, _) = Create(Snapshot());
        viewModel.SearchText = "photos";
        IReadOnlyList<ProgramRowViewModel> rowsBefore = viewModel.Rows;

        // A cancelled scan leaves LastResult (and its evidence) unchanged but still raises the event.
        service.Raise();

        Assert.True(viewModel.HasResult);
        Assert.Same(rowsBefore, viewModel.Rows);
        Assert.Equal("photos", viewModel.SearchText);
    }
}

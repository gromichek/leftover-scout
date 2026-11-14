using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.Tests;

public class FindingsPageViewModelTests
{
    private const string Root = @"C:\Users\SampleUser";

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

        public void OpenRecycleBin()
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

    private sealed class MapReader : IDirectoryReader
    {
        private readonly Dictionary<string, DirectoryEntry[]> _folders = new(StringComparer.OrdinalIgnoreCase);

        public MapReader Folder(string path, params DirectoryEntry[] entries)
        {
            _folders[path] = entries;
            return this;
        }

        public DirectoryListing ReadDirectory(string path) =>
            DirectoryListing.Success(_folders.TryGetValue(path, out DirectoryEntry[]? entries) ? entries : []);
    }

    private static DirectoryEntry File(string name, long length, DateTime lastWriteUtc) =>
        new(name, false, length, lastWriteUtc, 0x20, 0);

    private static DirectoryEntry Dir(string name) =>
        new(name, true, 0, DateTime.UnixEpoch, EntryClassifier.AttributeDirectory, 0);

    private static readonly DateTime Old = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private const string CacheRules = """
        { "schemaVersion": 1, "rules": [
          { "id": "sample-cache", "kind": "cache", "paths": ["{RoamingAppData}\\*\\Cache"], "category": "cache", "scope": "folder",
            "program": { "displayName": "Foo Soft", "requiresInstallation": true }, "description": "Sample cache.", "source": "test" },
          { "id": "sample-temp", "kind": "cache", "paths": ["{LocalAppData}\\Temp"], "category": "temp", "scope": "folder",
            "description": "Sample temp.", "source": "test" } ] }
        """;

    /// <summary>
    /// FooSoft (1400 B, old) with a Cache folder inside (400 B) → Medium leftover with a nested cache;
    /// BarTool (50 B, changed just now) → Low leftover; Local\Temp (300 B) → Temp.
    /// </summary>
    private static ScanResult Result(string rulesJson = CacheRules)
    {
        var reader = new MapReader()
            .Folder(Root, Dir("AppData"))
            .Folder(Root + @"\AppData", Dir("Roaming"), Dir("Local"))
            .Folder(Root + @"\AppData\Roaming", Dir("FooSoft"))
            .Folder(Root + @"\AppData\Roaming\FooSoft", Dir("Cache"), File("settings.dat", 1000, Old))
            .Folder(Root + @"\AppData\Roaming\FooSoft\Cache", File("blob.bin", 400, Old))
            .Folder(Root + @"\AppData\Local", Dir("BarTool"), Dir("Temp"))
            .Folder(Root + @"\AppData\Local\BarTool", File("bar.dat", 50, DateTime.UtcNow.AddDays(-1)))
            .Folder(Root + @"\AppData\Local\Temp", File("x.tmp", 300, Old));
        ScanResult scan = new ProfileScanner(reader)
            .ScanAsync(new ScanTarget(Root, true), new ScanOptions(), null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        RuleLoadResult rules = RuleLoader.Load([new RuleFile("test.json", rulesJson)]);
        Assert.Empty(rules.Errors);
        RuleMatchMap matches = new RuleMatcher(rules.RuleSet).Match(scan.Root!, CancellationToken.None);
        var evidence = new EvidenceSnapshot([], new Dictionary<ProgramSource, int>(), [], DateTimeOffset.UnixEpoch, TimeSpan.Zero);
        LeftoverMap leftovers = new LeftoverClassifier(TimeProvider.System).Classify(scan.Root!, evidence, matches, CancellationToken.None);
        return scan with { CompletedAt = new DateTimeOffset(2025, 3, 4, 12, 30, 0, TimeSpan.Zero), RuleMatches = matches, Leftovers = leftovers };
    }

    private static (FindingsPageViewModel ViewModel, StubScanService Service, RecordingShell Shell, FakeClipboard Clipboard) Create(ScanResult? last = null)
    {
        var service = new StubScanService { LastResult = last };
        var shell = new RecordingShell();
        var clipboard = new FakeClipboard();
        return (new FindingsPageViewModel(service, shell, clipboard), service, shell, clipboard);
    }

    private static FindingRowViewModel RowFor(FindingsPageViewModel viewModel, string relativePath) =>
        viewModel.Rows.Single(row => row.Path == relativePath);

    [Fact]
    public void Constructor_NoScanYet_ShowsNoResultState()
    {
        var (viewModel, _, _, _) = Create();

        Assert.False(viewModel.HasResult);
        Assert.True(viewModel.HasNoResult);
        Assert.Empty(viewModel.Rows);
        Assert.Equal("findings", viewModel.HelpTopicId);
        Assert.Equal("Findings", viewModel.Title);
    }

    [Fact]
    public void GoToScan_Command_RaisesRequest()
    {
        var (viewModel, _, _, _) = Create();
        int raised = 0;
        viewModel.GoToScanRequested += (_, _) => raised++;

        viewModel.GoToScanCommand.Execute(null);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Constructor_WithResult_ShowsHeaderAndDefaultFilter()
    {
        var (viewModel, _, _, _) = Create(Result());

        Assert.True(viewModel.HasResult);
        Assert.StartsWith("Scanned ", viewModel.HeaderText, StringComparison.Ordinal);
        Assert.True(viewModel.ShowHigh && viewModel.ShowMedium && viewModel.ShowCache && viewModel.ShowTemp && viewModel.ShowLogs && viewModel.ShowCrashDumps);
        Assert.False(viewModel.ShowLow);
        Assert.Equal(string.Empty, viewModel.SearchText);
        Assert.Equal("Size", FindingsPageViewModel.DefaultSortMember);
        Assert.Equal(System.ComponentModel.ListSortDirection.Descending, FindingsPageViewModel.DefaultSortDirection);
    }

    [Fact]
    public void Rows_Default_AreSortedBySizeDescendingAndLowIsHidden()
    {
        var (viewModel, _, _, _) = Create(Result());

        Assert.Equal(
            [@"AppData\Roaming\FooSoft", @"AppData\Roaming\FooSoft\Cache", @"AppData\Local\Temp"],
            viewModel.Rows.Select(row => row.Path));
        Assert.Equal([1400L, 400L, 300L], viewModel.Rows.Select(row => row.Size));
    }

    [Fact]
    public void Rows_ExposeRawAndFormattedValues()
    {
        var (viewModel, _, _, _) = Create(Result());

        FindingRowViewModel foo = RowFor(viewModel, @"AppData\Roaming\FooSoft");
        Assert.Equal("Leftover", foo.KindText);
        Assert.Equal("Medium", foo.ConfidenceText);
        Assert.Equal(2, foo.ConfidenceRank);
        Assert.Equal(SizeFormatter.Format(1400), foo.SizeText);
        Assert.Equal(Old, foo.LastModified);

        FindingRowViewModel cache = RowFor(viewModel, @"AppData\Roaming\FooSoft\Cache");
        Assert.Equal("Cache", cache.KindText);
        Assert.Equal(string.Empty, cache.ConfidenceText);
        Assert.Equal(0, cache.ConfidenceRank);
        Assert.Equal("Foo Soft", cache.Program);
    }

    [Fact]
    public void TotalText_CountsVisibleItemsButNotNestedSizes()
    {
        var (viewModel, _, _, _) = Create(Result());

        // FooSoft 1400 (includes its Cache) + Temp 300; the nested Cache (400) is shown but not added again.
        Assert.Equal($"3 items · {SizeFormatter.Format(1700)}", viewModel.TotalText);
    }

    [Fact]
    public void ShowLow_Ticked_AddsLowLeftover()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.ShowLow = true;

        Assert.Equal(4, viewModel.Rows.Count);
        Assert.Equal("Low", RowFor(viewModel, @"AppData\Local\BarTool").ConfidenceText);
        Assert.Equal($"4 items · {SizeFormatter.Format(1750)}", viewModel.TotalText);
    }

    [Fact]
    public void ShowCache_Unticked_RemovesCacheRowsAndChangesTotal()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.ShowCache = false;

        Assert.DoesNotContain(viewModel.Rows, row => row.KindText == "Cache");
        Assert.Equal($"2 items · {SizeFormatter.Format(1700)}", viewModel.TotalText);

        viewModel.ShowMedium = false;

        Assert.Equal(["Temp"], viewModel.Rows.Select(row => row.KindText));
        Assert.Equal($"1 item · {SizeFormatter.Format(300)}", viewModel.TotalText);
    }

    [Fact]
    public void ShowMedium_Unticked_CountsTheNestedCacheItself()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.ShowMedium = false;

        FindingRowViewModel cache = RowFor(viewModel, @"AppData\Roaming\FooSoft\Cache");
        Assert.False(cache.IsNested);
        Assert.Equal($"2 items · {SizeFormatter.Format(700)}", viewModel.TotalText);
    }

    [Fact]
    public void SearchText_FiltersByPathOrProgram_IgnoringCase()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.SearchText = "FOO soft";
        Assert.Equal([@"AppData\Roaming\FooSoft\Cache"], viewModel.Rows.Select(row => row.Path));

        viewModel.SearchText = "temp";
        Assert.Equal([@"AppData\Local\Temp"], viewModel.Rows.Select(row => row.Path));
    }

    [Fact]
    public void Filters_NothingMatches_ShowsEmptyMessage()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.SearchText = "no such thing";

        Assert.Empty(viewModel.Rows);
        Assert.True(viewModel.HasEmptyMessage);
        Assert.Equal("No findings match the current filters.", viewModel.EmptyMessage);
        Assert.Equal($"0 items · {SizeFormatter.Format(0)}", viewModel.TotalText);
    }

    [Fact]
    public async Task Result_WithoutFindings_ShowsNothingFoundMessage()
    {
        var reader = new MapReader().Folder(Root, File("a.txt", 5, Old));
        ScanResult scan = await new ProfileScanner(reader)
            .ScanAsync(new ScanTarget(Root, true), new ScanOptions(), null, TestContext.Current.CancellationToken);

        var (viewModel, _, _, _) = Create(scan);

        Assert.True(viewModel.HasResult);
        Assert.Empty(viewModel.Rows);
        Assert.Equal("Nothing was found that you could remove.", viewModel.EmptyMessage);
    }

    [Fact]
    public void SelectedRow_Details_ExplainWhyAndNoteNesting()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.SelectedRow = RowFor(viewModel, @"AppData\Roaming\FooSoft\Cache");

        FindingRowViewModel selected = viewModel.SelectedRow!;
        Assert.True(viewModel.HasSelection);
        Assert.False(viewModel.HasNoSelection);
        Assert.Equal(Root + @"\AppData\Roaming\FooSoft\Cache", selected.FullPath);
        Assert.Contains("Data that a program can download or build again.", selected.ExplanationText, StringComparison.Ordinal);
        Assert.Equal("Sample cache. (sample-cache)", selected.RuleText);
        Assert.True(selected.HasRule);
        Assert.Equal(@"Inside another finding: AppData\Roaming\FooSoft", selected.InsideNote);
        Assert.True(selected.IsNested);

        FindingRowViewModel leftover = RowFor(viewModel, @"AppData\Roaming\FooSoft");
        Assert.Contains("No installed program named 'FooSoft' was found.", leftover.ExplanationText, StringComparison.Ordinal);
        Assert.Equal(string.Empty, leftover.InsideNote);
    }

    [Fact]
    public void NoSelection_ShowsEmptyDetailsText()
    {
        var (viewModel, _, _, _) = Create(Result());

        Assert.True(viewModel.HasNoSelection);
        Assert.Equal("Select an item to see why it is listed.", viewModel.NoSelectionMessage);
        Assert.False(viewModel.OpenInExplorerCommand.CanExecute(null));
        Assert.False(viewModel.CopyPathCommand.CanExecute(null));
        Assert.False(viewModel.ShowInResultsCommand.CanExecute(null));
    }

    [Fact]
    public void OpenInExplorer_OpensTheFolder()
    {
        var (viewModel, _, shell, _) = Create(Result());
        viewModel.SelectedRow = RowFor(viewModel, @"AppData\Local\Temp");

        viewModel.OpenInExplorerCommand.Execute(null);

        Assert.Equal([Root + @"\AppData\Local\Temp"], shell.OpenedFolders);
    }

    [Fact]
    public void CopyPath_PutsFullPathOnClipboard_AndReportsFailure()
    {
        var (viewModel, _, _, clipboard) = Create(Result());
        viewModel.SelectedRow = RowFor(viewModel, @"AppData\Local\Temp");

        viewModel.CopyPathCommand.Execute(null);
        Assert.Equal([Root + @"\AppData\Local\Temp"], clipboard.Texts);
        Assert.Equal("Path copied to the clipboard", viewModel.StatusMessage);

        clipboard.Succeeds = false;
        viewModel.CopyPathCommand.Execute(null);
        Assert.Equal("The path could not be copied because the clipboard is busy", viewModel.StatusMessage);
    }

    [Fact]
    public void ShowInResults_RaisesRequestWithTheFolder()
    {
        var (viewModel, _, _, _) = Create(Result());
        viewModel.SelectedRow = RowFor(viewModel, @"AppData\Local\Temp");
        FolderNode? requested = null;
        viewModel.ShowInResultsRequested += (_, node) => requested = node;

        viewModel.ShowInResultsCommand.Execute(null);

        Assert.Same(viewModel.SelectedRow!.Finding.Node, requested);
    }

    [Fact]
    public void LastResultChanged_SameResult_KeepsSelectionAndFilters()
    {
        var (viewModel, service, _, _) = Create(Result());
        viewModel.ShowLow = true;
        viewModel.SelectedRow = RowFor(viewModel, @"AppData\Local\BarTool");

        service.Raise(); // a cancelled scan: LastResult is unchanged (decision D-039)

        Assert.Equal(4, viewModel.Rows.Count);
        Assert.NotNull(viewModel.SelectedRow);
    }

    [Fact]
    public void LastResultChanged_NewResult_ReplacesRowsButKeepsFilters()
    {
        var (viewModel, service, _, _) = Create();
        viewModel.ShowLow = true;
        viewModel.SearchText = "bar";

        service.LastResult = Result();
        service.Raise();

        Assert.True(viewModel.HasResult);
        Assert.Equal([@"AppData\Local\BarTool"], viewModel.Rows.Select(row => row.Path));
    }

    [Fact]
    public void ShowInResults_ThroughMainViewModel_OpensResultsAtTheParentWithTheRowSelected()
    {
        ScanResult scan = Result();
        var service = new StubScanService { LastResult = scan };
        var results = new ResultsPageViewModel(service, new RecordingShell(), new FakeClipboard());
        var findings = new FindingsPageViewModel(service, new RecordingShell(), new FakeClipboard());
        var main = new MainViewModel([results, findings], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        main.CurrentPage = findings;
        findings.SelectedRow = RowFor(findings, @"AppData\Roaming\FooSoft\Cache");

        findings.ShowInResultsCommand.Execute(null);

        Assert.Same(results, main.CurrentPage);
        Assert.Equal(["SampleUser", "AppData", "Roaming", "FooSoft"], results.Breadcrumb.Select(item => item.Label));
        Assert.Equal("Cache", results.SelectedRow?.Name);
    }

    [Fact]
    public void GoToScan_ThroughMainViewModel_SwitchesToTheScanPage()
    {
        var service = new StubScanService();
        var findings = new FindingsPageViewModel(service, new RecordingShell(), new FakeClipboard());
        var scanPage = new ScanPageViewModel(service);
        var main = new MainViewModel([scanPage, findings], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        main.CurrentPage = findings;

        findings.GoToScanCommand.Execute(null);

        Assert.Same(scanPage, main.CurrentPage);
    }

    // ---- Selection and "Clean up selected…" (decisions D-056, D-068, D-069) ----

    private static (FindingsPageViewModel ViewModel, StubScanService Service, CleanupState State) CreateWithState(ScanResult last)
    {
        var service = new StubScanService { LastResult = last };
        var state = new CleanupState();
        return (new FindingsPageViewModel(service, new RecordingShell(), new FakeClipboard(), state), service, state);
    }

    [Fact]
    public void Selection_Default_NothingIsSelected()
    {
        var (viewModel, _, _) = CreateWithState(Result());

        Assert.All(viewModel.Rows, row => Assert.False(row.IsSelected));
        Assert.Equal(0, viewModel.SelectedCount);
        Assert.Equal("Selected: 0 items · 0 B", viewModel.SelectionText);
        Assert.False(viewModel.CleanUpSelectedCommand.CanExecute(null));
        Assert.False(viewModel.ClearSelectionCommand.CanExecute(null));
    }

    [Fact]
    public void Selection_TickingARow_UpdatesTheLineAndEnablesCleanUp()
    {
        var (viewModel, _, _) = CreateWithState(Result());

        RowFor(viewModel, @"AppData\Local\Temp").IsSelected = true;

        Assert.Equal(1, viewModel.SelectedCount);
        Assert.Equal($"Selected: 1 item · {SizeFormatter.Format(300)}", viewModel.SelectionText);
        Assert.True(viewModel.CleanUpSelectedCommand.CanExecute(null));
        Assert.True(viewModel.ClearSelectionCommand.CanExecute(null));
    }

    [Fact]
    public void ToggleSelectedRow_FlipsTheCheckboxOfTheSelectedRow()
    {
        var (viewModel, _, _) = CreateWithState(Result());
        Assert.False(viewModel.ToggleSelectedRowCommand.CanExecute(null));
        viewModel.SelectedRow = RowFor(viewModel, @"AppData\Local\Temp");

        viewModel.ToggleSelectedRowCommand.Execute(null);
        Assert.True(viewModel.SelectedRow.IsSelected);
        Assert.Equal(1, viewModel.SelectedCount);

        viewModel.ToggleSelectedRowCommand.Execute(null);
        Assert.False(viewModel.SelectedRow.IsSelected);
        Assert.Equal(0, viewModel.SelectedCount);
    }

    [Fact]
    public void SelectAllVisible_SelectsEveryVisibleRow_AndClearSelectionClears()
    {
        var (viewModel, _, _) = CreateWithState(Result());

        viewModel.SelectAllVisibleCommand.Execute(null);

        Assert.Equal(3, viewModel.SelectedCount);
        Assert.All(viewModel.Rows, row => Assert.True(row.IsSelected));

        viewModel.ClearSelectionCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(viewModel.Rows, row => Assert.False(row.IsSelected));
        Assert.Equal("Selected: 0 items · 0 B", viewModel.SelectionText);
    }

    [Fact]
    public void SelectionLine_NestedSelectedItems_AreNotCountedTwice()
    {
        var (viewModel, _, _) = CreateWithState(Result());

        RowFor(viewModel, @"AppData\Roaming\FooSoft").IsSelected = true;
        RowFor(viewModel, @"AppData\Roaming\FooSoft\Cache").IsSelected = true;

        Assert.Equal(2, viewModel.SelectedCount);
        Assert.Equal($"Selected: 2 items · {SizeFormatter.Format(1400)}", viewModel.SelectionText);
    }

    [Fact]
    public void FilterChange_DeselectsItemsThatBecomeHidden()
    {
        var (viewModel, _, _) = CreateWithState(Result());
        viewModel.ShowLow = true;
        RowFor(viewModel, @"AppData\Local\BarTool").IsSelected = true;
        RowFor(viewModel, @"AppData\Local\Temp").IsSelected = true;
        Assert.Equal(2, viewModel.SelectedCount);

        viewModel.ShowLow = false;

        Assert.Equal(1, viewModel.SelectedCount);
        Assert.True(RowFor(viewModel, @"AppData\Local\Temp").IsSelected);

        viewModel.ShowLow = true;

        Assert.False(RowFor(viewModel, @"AppData\Local\BarTool").IsSelected);
        Assert.Equal(1, viewModel.SelectedCount);
    }

    [Fact]
    public void FilterChange_KeepsSelectedItemsThatStayVisible()
    {
        var (viewModel, _, _) = CreateWithState(Result());
        RowFor(viewModel, @"AppData\Local\Temp").IsSelected = true;

        viewModel.SearchText = "temp";

        Assert.True(RowFor(viewModel, @"AppData\Local\Temp").IsSelected);
        Assert.Equal(1, viewModel.SelectedCount);

        viewModel.SearchText = "foo";

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.False(viewModel.CleanUpSelectedCommand.CanExecute(null));
    }

    [Fact]
    public void NewScan_ClearsTheSelection()
    {
        var (viewModel, service, _) = CreateWithState(Result());
        viewModel.SelectAllVisibleCommand.Execute(null);

        service.LastResult = Result();
        service.Raise();

        Assert.Equal(0, viewModel.SelectedCount);
        Assert.All(viewModel.Rows, row => Assert.False(row.IsSelected));
    }

    [Fact]
    public void CleanUpSelected_RaisesRequestWithTheSelectedItems()
    {
        ScanResult scan = Result();
        var (viewModel, _, _) = CreateWithState(scan);
        viewModel.ShowLow = true;
        RowFor(viewModel, @"AppData\Roaming\FooSoft").IsSelected = true;
        RowFor(viewModel, @"AppData\Local\BarTool").IsSelected = true;
        CleanupRequestedEventArgs? args = null;
        viewModel.CleanupRequested += (_, e) => args = e;

        viewModel.CleanUpSelectedCommand.Execute(null);

        Assert.NotNull(args);
        Assert.Same(scan, args.SourceResult);
        Assert.Equal(Root, args.Request.Target.ProfileRootPath);
        Assert.Equal(
            [Root + @"\AppData\Roaming\FooSoft", Root + @"\AppData\Local\BarTool"],
            args.Request.Items.Select(item => item.FullPath).Order(StringComparer.Ordinal).Reverse());
        CleanupItem bar = args.Request.Items.Single(item => item.FullPath.EndsWith("BarTool", StringComparison.Ordinal));
        Assert.Equal(LeftoverConfidence.Low, bar.Confidence);
        Assert.Equal(50, bar.SizeBytes);
    }

    [Fact]
    public void CleanUpSelected_AfterACleanup_IsDisabledUntilANewScanCompletes()
    {
        ScanResult scan = Result();
        var (viewModel, service, state) = CreateWithState(scan);
        RowFor(viewModel, @"AppData\Local\Temp").IsSelected = true;
        Assert.True(viewModel.CleanUpSelectedCommand.CanExecute(null));
        Assert.False(viewModel.IsResultStale);

        state.MarkStale(scan);

        Assert.True(viewModel.IsResultStale);
        Assert.Equal("Results are out of date after a cleanup — scan again.", viewModel.StaleMessage);
        Assert.False(viewModel.CleanUpSelectedCommand.CanExecute(null));

        service.LastResult = Result();
        service.Raise();
        RowFor(viewModel, @"AppData\Local\Temp").IsSelected = true;

        Assert.False(viewModel.IsResultStale);
        Assert.True(viewModel.CleanUpSelectedCommand.CanExecute(null));
    }

    [Fact]
    public void CleanUpSelected_WhileACleanupRuns_IsDisabled()
    {
        var (viewModel, _, state) = CreateWithState(Result());
        RowFor(viewModel, @"AppData\Local\Temp").IsSelected = true;

        state.IsRunning = true;

        Assert.False(viewModel.CleanUpSelectedCommand.CanExecute(null));
    }

    [Fact]
    public void CleanUpSelected_ThroughMainViewModel_OpensTheCleanupPage()
    {
        ScanResult scan = Result();
        var service = new StubScanService { LastResult = scan };
        var state = new CleanupState();
        var findings = new FindingsPageViewModel(service, new RecordingShell(), new FakeClipboard(), state);
        var cleanup = new CleanupPageViewModel(new AllowAllGuard(), new NoRunningPrograms(), new NeverRunService(), new NoDialogs(), state, new RecordingShell());
        var main = new MainViewModel([findings, cleanup], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        main.CurrentPage = findings;
        RowFor(findings, @"AppData\Local\Temp").IsSelected = true;

        findings.CleanUpSelectedCommand.Execute(null);

        Assert.Same(cleanup, main.CurrentPage);
        Assert.False(cleanup.IsNothingSelected);
    }

    [Fact]
    public void ResultsPage_AfterACleanup_ShowsTheOutOfDateBanner()
    {
        ScanResult scan = Result();
        var service = new StubScanService { LastResult = scan };
        var state = new CleanupState();
        var results = new ResultsPageViewModel(service, new RecordingShell(), new FakeClipboard(), state);
        Assert.False(results.IsResultStale);

        state.MarkStale(scan);

        Assert.True(results.IsResultStale);
        Assert.Equal("Results are out of date after a cleanup — scan again.", results.StaleMessage);

        service.LastResult = Result();
        service.Raise();

        Assert.False(results.IsResultStale);
    }

    private sealed class AllowAllGuard : ICleanupGuard
    {
        public CleanupGuardReport Check(CleanupRequest request)
        {
            RequestReduction reduction = RequestReducer.Reduce(request.Items);
            return new CleanupGuardReport(reduction, [.. reduction.Items.Select(item => new GuardItemResult(item, new GuardResult([], [])))]);
        }
    }

    private sealed class NoRunningPrograms : IRunningProgramDetector
    {
        public IReadOnlyList<RunningProgramWarning> Find(CleanupRequest request) => [];
    }

    private sealed class NeverRunService : ICleanupService
    {
        public Task<CleanupSessionResult> RunAsync(
            CleanupRequest request,
            CleanupMode mode,
            IProgress<CleanupProgress>? progress,
            CancellationToken cancellationToken,
            Action<CleanupItemResult>? itemCompleted = null) => throw new NotSupportedException();
    }

    private sealed class NoDialogs : LeftoverScout.App.Services.IDialogService
    {
        public bool Confirm(string title, string message) => false;
    }
}

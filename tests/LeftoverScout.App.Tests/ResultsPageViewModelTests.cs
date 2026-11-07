using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.Tests;

public class ResultsPageViewModelTests
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

        public List<string> SelectedFiles { get; } = [];

        public void OpenFolder(string folderPath) => OpenedFolders.Add(folderPath);

        public void SelectFile(string filePath) => SelectedFiles.Add(filePath);

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

    private static DirectoryEntry File(string name, long length, int year = 2024) =>
        new(name, false, length, new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0x20, 0);

    private static DirectoryEntry Dir(string name) =>
        new(name, true, 0, DateTime.UnixEpoch, EntryClassifier.AttributeDirectory, 0);

    private static DirectoryEntry Junction(string name) =>
        new(name, true, 0, DateTime.UnixEpoch, EntryClassifier.AttributeDirectory | EntryClassifier.AttributeReparsePoint, EntryClassifier.ReparseTagMountPoint);

    private static ScanResult Result(long bigFile = 300)
    {
        var reader = new MapReader()
            .Folder(Root, Dir("AppData"), Dir("Videos"), Junction("Application Data"), File("root.txt", 100))
            .Folder(Root + @"\AppData", Dir("Local"), File("a.bin", 10))
            .Folder(Root + @"\AppData\Local", File("cache.bin", bigFile))
            .Folder(Root + @"\Videos", File("movie.mkv", 5000));
        ScanResult result = new ProfileScanner(reader)
            .ScanAsync(new ScanTarget(Root, true), new ScanOptions { LargeFileThresholdBytes = 300 }, null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return result with { CompletedAt = new DateTimeOffset(2025, 3, 4, 12, 30, 0, TimeSpan.Zero) };
    }

    private static ScanResult ResultWithoutLargeFiles()
    {
        var reader = new MapReader().Folder(Root, File("small.txt", 1));
        return new ProfileScanner(reader)
            .ScanAsync(new ScanTarget(Root, true), new ScanOptions(), null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static (ResultsPageViewModel ViewModel, StubScanService Service, RecordingShell Shell, FakeClipboard Clipboard) Create(ScanResult? last = null)
    {
        var service = new StubScanService { LastResult = last };
        var shell = new RecordingShell();
        var clipboard = new FakeClipboard();
        return (new ResultsPageViewModel(service, shell, clipboard), service, shell, clipboard);
    }

    private static FolderRowViewModel RowNamed(ResultsPageViewModel viewModel, string name) =>
        viewModel.Rows.Single(row => row.Name == name);

    [Fact]
    public void Constructor_NoScanYet_ShowsNoResultState()
    {
        var (viewModel, _, _, _) = Create();

        Assert.False(viewModel.HasResult);
        Assert.True(viewModel.HasNoResult);
        Assert.Empty(viewModel.Rows);
        Assert.Equal("results", viewModel.HelpTopicId);
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
    public void MainViewModel_GoToScanRequest_SwitchesToScanPage()
    {
        var scanPage = new ScanPageViewModel(new StubScanService());
        var (results, _, _, _) = Create();
        var main = new MainViewModel([scanPage, results], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        main.NavigateCommand.Execute(results);
        Assert.Same(results, main.CurrentPage);

        results.GoToScanCommand.Execute(null);

        Assert.Same(scanPage, main.CurrentPage);
    }

    [Fact]
    public void Constructor_WithResult_ShowsHeaderAndRootFoldersBiggestFirst()
    {
        var (viewModel, _, _, _) = Create(Result());

        Assert.True(viewModel.HasResult);
        Assert.Contains("local", viewModel.HeaderText);
        Assert.Contains("cloud-only", viewModel.HeaderText);
        Assert.StartsWith("Scanned ", viewModel.HeaderText);
        Assert.Contains(SizeFormatter.Format(5000 + 100 + 10 + 300), viewModel.HeaderText);
        Assert.Equal(new[] { "Videos", "AppData", "(files in this folder)", "Application Data" }, viewModel.Rows.Select(row => row.Name));
        Assert.Equal("SampleUser", Assert.Single(viewModel.Breadcrumb).Label);
        Assert.False(viewModel.UpCommand.CanExecute(null));
        Assert.Equal($"3 folders · {SizeFormatter.Format(5410)} in this folder", viewModel.FolderSummaryText);
    }

    [Fact]
    public void LastResultChanged_NewResult_ShowsItAndReturnsToRoot()
    {
        var (viewModel, service, _, _) = Create(Result());
        viewModel.OpenFolderCommand.Execute(RowNamed(viewModel, "AppData"));
        Assert.Equal(2, viewModel.Breadcrumb.Count);

        service.LastResult = Result(bigFile: 999);
        service.Raise();

        Assert.Single(viewModel.Breadcrumb);
        Assert.Contains(SizeFormatter.Format(5000 + 100 + 10 + 999), viewModel.HeaderText);
    }

    [Fact]
    public void LastResultChanged_FirstResultAfterNoScan_SwitchesFromNoResultState()
    {
        var (viewModel, service, _, _) = Create();

        service.LastResult = Result();
        service.Raise();

        Assert.True(viewModel.HasResult);
        Assert.False(viewModel.HasNoResult);
        Assert.NotEmpty(viewModel.Rows);
    }

    [Fact]
    public void LastResultChanged_CancelledScanKeepsPreviousResult_DoesNotResetNavigation()
    {
        var (viewModel, service, _, _) = Create(Result());
        viewModel.OpenFolderCommand.Execute(RowNamed(viewModel, "AppData"));

        service.Raise();

        Assert.Equal(2, viewModel.Breadcrumb.Count);
        Assert.True(viewModel.HasResult);
    }

    [Fact]
    public void LastResultChanged_RaisedOnBackgroundThread_UpdatesThroughUiContext()
    {
        var posted = new List<(SendOrPostCallback Callback, object? State)>();
        var context = new RecordingContext(posted);
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var (viewModel, service, _, _) = Create();
            service.LastResult = Result();

            var background = new Thread(service.Raise);
            background.Start();
            background.Join();

            Assert.False(viewModel.HasResult);
            Assert.Single(posted);

            posted[0].Callback(posted[0].State);

            Assert.True(viewModel.HasResult);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class RecordingContext(List<(SendOrPostCallback Callback, object? State)> posted) : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) => posted.Add((d, state));
    }

    [Fact]
    public void OpenFolder_OpenableRow_NavigatesAndUpGoesBack()
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.OpenFolderCommand.Execute(RowNamed(viewModel, "AppData"));

        Assert.Equal(new[] { "SampleUser", "AppData" }, viewModel.Breadcrumb.Select(item => item.Label));
        Assert.Equal(new[] { "Local", "(files in this folder)" }, viewModel.Rows.Select(row => row.Name));
        Assert.True(viewModel.UpCommand.CanExecute(null));
        Assert.Equal($"1 folder · {SizeFormatter.Format(310)} in this folder", viewModel.FolderSummaryText);

        viewModel.UpCommand.Execute(null);

        Assert.Single(viewModel.Breadcrumb);
        Assert.False(viewModel.UpCommand.CanExecute(null));
    }

    [Fact]
    public void OpenFolder_NoParameter_UsesSelectedRow()
    {
        var (viewModel, _, _, _) = Create(Result());
        viewModel.SelectedRow = RowNamed(viewModel, "AppData");

        viewModel.OpenFolderCommand.Execute(null);

        Assert.Equal(2, viewModel.Breadcrumb.Count);
        Assert.Null(viewModel.SelectedRow);
    }

    [Theory]
    [InlineData("Application Data")]
    [InlineData("(files in this folder)")]
    public void OpenFolder_RowThatCannotOpen_DoesNothing(string name)
    {
        var (viewModel, _, _, _) = Create(Result());

        viewModel.OpenFolderCommand.Execute(RowNamed(viewModel, name));

        Assert.Single(viewModel.Breadcrumb);
    }

    [Fact]
    public void NavigateTo_BreadcrumbRoot_ReturnsToRoot()
    {
        var (viewModel, _, _, _) = Create(Result());
        viewModel.OpenFolderCommand.Execute(RowNamed(viewModel, "AppData"));
        viewModel.OpenFolderCommand.Execute(RowNamed(viewModel, "Local"));
        Assert.Equal(3, viewModel.Breadcrumb.Count);

        viewModel.NavigateToCommand.Execute(viewModel.Breadcrumb[0]);

        Assert.Single(viewModel.Breadcrumb);
        Assert.Equal(4, viewModel.Rows.Count);
    }

    [Fact]
    public void Rows_ExposeRawValuesForSortingAndFormattedTextForDisplay()
    {
        var (viewModel, _, _, _) = Create(Result());
        FolderRowViewModel videos = RowNamed(viewModel, "Videos");

        Assert.Equal(5000, videos.Size);
        Assert.Equal(SizeFormatter.Format(5000), videos.SizeText);
        Assert.Equal(1, videos.Files);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), videos.LastModified);
        Assert.Equal(5000 * 100.0 / 5410, videos.PercentOfParent, 6);
        Assert.EndsWith(" %", videos.PercentText);

        // A bigger folder in bytes must sort above a smaller one even when its text would sort lower.
        Assert.True(RowNamed(viewModel, "AppData").Size < videos.Size);
    }

    [Fact]
    public void Rows_LinkRow_HasNotesAndTooltip()
    {
        var (viewModel, _, _, _) = Create(Result());

        FolderRowViewModel link = RowNamed(viewModel, "Application Data");

        Assert.Equal("Link", link.NotesText);
        Assert.StartsWith("Link:", link.NotesTooltip);
        Assert.False(link.CanOpen);
        Assert.Equal(string.Empty, link.LastModifiedText);
    }

    [Fact]
    public void LargeFiles_ShowRelativePathsBiggestFirst()
    {
        var (viewModel, _, _, _) = Create(Result());

        Assert.Equal(new[] { "Videos\\movie.mkv", "AppData\\Local\\cache.bin" }, viewModel.LargeFiles.Select(file => file.RelativePath));
        Assert.Equal(Root + @"\Videos\movie.mkv", viewModel.LargeFiles[0].FullPath);
        Assert.Equal(5000, viewModel.LargeFiles[0].Size);
        Assert.True(viewModel.HasLargeFiles);
        Assert.False(viewModel.HasNoLargeFiles);
    }

    [Fact]
    public void LargeFiles_NoneFound_ShowsEmptyState()
    {
        var (viewModel, _, _, _) = Create(ResultWithoutLargeFiles());

        Assert.Empty(viewModel.LargeFiles);
        Assert.True(viewModel.HasNoLargeFiles);
        Assert.Equal("No files of 100 MB or more.", viewModel.NoLargeFilesMessage);
    }

    [Fact]
    public void FolderActions_DisabledWithoutSelection_AndActOnSelectedRow()
    {
        var (viewModel, _, shell, clipboard) = Create(Result());
        Assert.False(viewModel.OpenFolderInExplorerCommand.CanExecute(null));
        Assert.False(viewModel.CopyFolderPathCommand.CanExecute(null));

        viewModel.SelectedRow = RowNamed(viewModel, "Videos");
        viewModel.OpenFolderInExplorerCommand.Execute(null);
        viewModel.CopyFolderPathCommand.Execute(null);

        Assert.True(viewModel.OpenFolderInExplorerCommand.CanExecute(null));
        Assert.Equal(Root + @"\Videos", Assert.Single(shell.OpenedFolders));
        Assert.Equal(Root + @"\Videos", Assert.Single(clipboard.Texts));
        Assert.Equal("Path copied to the clipboard", viewModel.StatusMessage);
    }

    [Fact]
    public void FolderActions_FilesRow_UsesCurrentFolderPath()
    {
        var (viewModel, _, shell, _) = Create(Result());
        viewModel.SelectedRow = RowNamed(viewModel, "(files in this folder)");

        viewModel.OpenFolderInExplorerCommand.Execute(null);

        Assert.Equal(Root, Assert.Single(shell.OpenedFolders));
    }

    [Fact]
    public void FileActions_SelectFileInExplorerAndCopyPath()
    {
        var (viewModel, _, shell, clipboard) = Create(Result());
        Assert.False(viewModel.OpenFileInExplorerCommand.CanExecute(null));

        viewModel.SelectedLargeFile = viewModel.LargeFiles[0];
        viewModel.OpenFileInExplorerCommand.Execute(null);
        viewModel.CopyFilePathCommand.Execute(null);

        Assert.Equal(Root + @"\Videos\movie.mkv", Assert.Single(shell.SelectedFiles));
        Assert.Empty(shell.OpenedFolders);
        Assert.Equal(Root + @"\Videos\movie.mkv", Assert.Single(clipboard.Texts));
    }

    [Fact]
    public void CopyPath_ClipboardBusy_ReportsFailureWithoutThrowing()
    {
        var (viewModel, _, _, clipboard) = Create(Result());
        clipboard.Succeeds = false;
        viewModel.SelectedRow = RowNamed(viewModel, "Videos");

        viewModel.CopyFolderPathCommand.Execute(null);

        Assert.Contains("could not be copied", viewModel.StatusMessage);
    }
}

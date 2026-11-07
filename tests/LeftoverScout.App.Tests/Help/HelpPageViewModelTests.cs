using System.Globalization;
using LeftoverScout.App.Composition;
using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Settings;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.App.Tests.Help;

public class HelpPageViewModelTests
{
    private sealed class FakeSource : IHelpSource
    {
        public string? TryRead(HelpLanguage language, string fileName) => $"{HelpLanguages.ToCode(language)}:{fileName}";
    }

    private sealed class MemorySettings : ISettingsStore
    {
        public AppSettings Current { get; set; } = new();

        public int SaveCount { get; private set; }

        public AppSettings Load() => Current;

        public void Save(AppSettings settings)
        {
            Current = settings;
            SaveCount++;
        }
    }

    private sealed class RecordingShell : IShellLauncher
    {
        public List<string> OpenedUrls { get; } = [];

        public void OpenFolder(string folderPath)
        {
        }

        public void SelectFile(string filePath)
        {
        }

        public void OpenUrl(string url) => OpenedUrls.Add(url);
    }

    private static (HelpPageViewModel ViewModel, MemorySettings Settings, RecordingShell Shell) Create(string culture, string? savedLanguage = null)
    {
        var settings = new MemorySettings { Current = new AppSettings(HelpLanguage: savedLanguage) };
        var shell = new RecordingShell();
        var catalog = new HelpCatalog(new FakeSource(), NullLogger<HelpCatalog>.Instance);
        return (new HelpPageViewModel(catalog, settings, shell, new CultureInfo(culture)), settings, shell);
    }

    [Fact]
    public void Constructor_RussianWindows_ShowsRussianOverview()
    {
        var (viewModel, _, _) = Create("ru-RU");

        Assert.True(viewModel.IsRussian);
        Assert.Equal("ru:index.md", viewModel.Markdown);
        Assert.Equal(HelpTopics.Help, viewModel.HelpTopicId);
    }

    [Fact]
    public void Constructor_OtherWindows_ShowsEnglish()
    {
        var (viewModel, _, _) = Create("de-DE");

        Assert.True(viewModel.IsEnglish);
        Assert.Equal("en:index.md", viewModel.Markdown);
    }

    [Fact]
    public void Constructor_SavedLanguage_OverridesWindowsLanguage()
    {
        var (viewModel, _, _) = Create("ru-RU", savedLanguage: "en");

        Assert.True(viewModel.IsEnglish);
    }

    [Fact]
    public void SwitchingLanguage_ChangesTextAndSavesChoice()
    {
        var (viewModel, settings, _) = Create("en-US");
        viewModel.ShowTopic(HelpTopics.Scan);

        viewModel.IsRussian = true;

        Assert.Equal("ru:scan.md", viewModel.Markdown);
        Assert.False(viewModel.IsEnglish);
        Assert.Equal("ru", settings.Current.HelpLanguage);
    }

    [Fact]
    public void SwitchingLanguage_KeepsOtherSettings()
    {
        var (viewModel, settings, _) = Create("en-US");
        var window = new WindowPlacement(1, 2, 3, 4, false);
        settings.Current = settings.Current with { MainWindow = window };

        viewModel.IsRussian = true;

        Assert.Equal(window, settings.Current.MainWindow);
    }

    [Fact]
    public void SettingSameLanguage_DoesNotSave()
    {
        var (viewModel, settings, _) = Create("en-US");

        viewModel.IsEnglish = true;

        Assert.Equal(0, settings.SaveCount);
    }

    [Fact]
    public void OpenLink_TopicFile_NavigatesInsideHelp()
    {
        var (viewModel, _, shell) = Create("en-US");

        viewModel.OpenLink("scan.md");

        Assert.Equal(HelpTopics.Scan, viewModel.SelectedTopic?.Id);
        Assert.Equal("en:scan.md", viewModel.Markdown);
        Assert.Empty(shell.OpenedUrls);
    }

    [Fact]
    public void OpenLink_Https_OpensBrowser_AndOtherLinksAreIgnored()
    {
        var (viewModel, _, shell) = Create("en-US");

        viewModel.OpenLink("https://example.com/");
        viewModel.OpenLink("file:///C:/secret.txt");
        viewModel.OpenLink("unknown.md");

        Assert.Equal(["https://example.com/"], shell.OpenedUrls);
        Assert.Equal(HelpTopics.Overview, viewModel.SelectedTopic?.Id);
    }

    [Fact]
    public void ShowTopic_UnknownId_ShowsOverview()
    {
        var (viewModel, _, _) = Create("en-US");
        viewModel.ShowTopic(HelpTopics.About);

        viewModel.ShowTopic("nope");

        Assert.Equal(HelpTopics.Overview, viewModel.SelectedTopic?.Id);
    }

    [Fact]
    public void OpenHelpCommand_OpensTopicOfCurrentPage()
    {
        var (help, _, _) = Create("en-US");
        var scan = new PlaceholderPageViewModel("Scan", HelpTopics.Scan, 1, "Scan description.");
        var main = new MainViewModel([scan, help], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));

        main.OpenHelpCommand.Execute(null);

        Assert.Same(help, main.CurrentPage);
        Assert.Equal(HelpTopics.Scan, help.SelectedTopic?.Id);
    }

    [Fact]
    public void OpenHelpCommand_OnHelpPage_OpensHelpTopic()
    {
        var (help, _, _) = Create("en-US");
        var scan = new PlaceholderPageViewModel("Scan", HelpTopics.Scan, 1, "Scan description.");
        var main = new MainViewModel([scan, help], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        main.CurrentPage = help;
        help.ShowTopic(HelpTopics.Cleanup);

        main.OpenHelpCommand.Execute(null);

        Assert.Equal(HelpTopics.Help, help.SelectedTopic?.Id);
    }

    [Fact]
    public void RegisteredPages_AllHaveHelpTopicsThatExist_AndHelpIsBetweenHistoryAndAbout()
    {
        DirectoryInfo temp = Directory.CreateTempSubdirectory("LeftoverScoutTests-");
        try
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services, new AppDataLocation(temp.FullName, IsFallback: false));
            using ServiceProvider provider = services.BuildServiceProvider();

            List<PageViewModel> pages = provider.GetServices<PageViewModel>().ToList();

            Assert.All(pages, page => Assert.NotNull(HelpTopics.FindById(page.HelpTopicId)));
            Assert.Equal(["Scan", "Results", "Cleanup", "History", "Help", "About"], pages.Select(p => p.Title));
        }
        finally
        {
            // The folder was created by this test in the system temp folder.
            temp.Delete(recursive: true);
        }
    }

    private sealed class NoScanService : IScanService
    {
        public ScanResult? LastResult => null;

        public Task<ScanResult> RunAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}

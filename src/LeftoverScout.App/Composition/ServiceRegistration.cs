using LeftoverScout.App.Help;
using LeftoverScout.App.Services;
using LeftoverScout.App.Startup;
using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Journal;
using LeftoverScout.Core.Logging;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Settings;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;
using LeftoverScout.Windows.Cleanup;
using LeftoverScout.Windows.Evidence;
using LeftoverScout.Windows.Scanning;
using LeftoverScout.Windows.Shell;
using LeftoverScout.Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.App.Composition;

/// <summary>
/// The single place where views, view models and services are registered.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>Creates the service provider for the running application.</summary>
    /// <param name="dataLocation">The resolved data folder.</param>
    public static ServiceProvider Build(AppDataLocation dataLocation)
    {
        var services = new ServiceCollection();
        Register(services, dataLocation);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    /// <summary>Registers all application services.</summary>
    public static void Register(IServiceCollection services, AppDataLocation dataLocation)
    {
        string logsFolder = System.IO.Path.Combine(dataLocation.Path, "logs");

        services.AddSingleton(dataLocation);

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new FileLoggerProvider(logsFolder));
        });

        // Services
        services.AddSingleton<ISettingsStore>(provider =>
            new JsonSettingsStore(dataLocation.Path, provider.GetRequiredService<ILogger<JsonSettingsStore>>()));
        services.AddSingleton<IShellLauncher, ShellLauncher>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<WindowPlacementService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(ScanTarget.ForCurrentUser());
        services.AddSingleton<IDirectoryReader, Win32DirectoryReader>();
        services.AddSingleton<IPersonalFolderProvider, PersonalFolderProvider>();
        services.AddSingleton<IFolderExistence, FileSystemFolderExistence>();

        // Evidence: installed programs (read-only), collected as part of every scan.
        services.AddSingleton<IEvidenceLocations, WindowsEvidenceLocations>();
        services.AddSingleton<IUninstallRegistry, WindowsUninstallRegistry>();
        services.AddSingleton<IStorePackageProvider, WinRtStorePackageProvider>();
        services.AddSingleton<IInstalledProgramSource, RegistryProgramSource>();
        services.AddSingleton<IInstalledProgramSource, StoreProgramSource>();
        services.AddSingleton<IInstalledProgramSource, ProgramFolderSource>();
        services.AddSingleton<IInstalledProgramSource, StartMenuSource>();
        services.AddSingleton<EvidenceCollector>();

        // Rules knowledge base (embedded JSON, validated at load time; invalid rules are skipped and logged).
        services.AddSingleton(provider => EmbeddedRuleLoader.Load(provider.GetRequiredService<ILogger<RuleSet>>()));
        services.AddSingleton<RuleMatcher>();
        services.AddSingleton<LeftoverClassifier>();

        services.AddSingleton<IScanService, ScanService>();

        // Cleanup (T-010) wrapped by the journal (T-012). No page starts a cleanup yet; the journal is already in place.
        string[] appFolders = [AppContext.BaseDirectory, dataLocation.Path];
        services.AddSingleton<IPathProbe, WindowsPathProbe>();
        services.AddSingleton<IRecycleBinSettingsReader, WindowsRecycleBinSettingsReader>();
        services.AddSingleton<IEntryRemover, ShellEntryRemover>();
        services.AddSingleton<IFolderRemover, FolderRemover>();
        services.AddSingleton<ICleanupGuard>(provider => new CleanupGuard(
            provider.GetRequiredService<RuleSet>(),
            provider.GetRequiredService<IPathProbe>(),
            provider.GetRequiredService<IDirectoryReader>(),
            provider.GetRequiredService<IPersonalFolderProvider>(),
            appFolders,
            provider.GetRequiredService<ILogger<CleanupGuard>>()));
        services.AddSingleton<CleanupService>();
        services.AddSingleton<ActiveJournalSessions>();
        services.AddSingleton<IJournalStorage>(new FileJournalStorage(dataLocation.Path));
        services.AddSingleton<IJournalReader, JournalReader>();
        services.AddSingleton<ICleanupJournal>(provider => new CleanupJournal(
            provider.GetRequiredService<IJournalStorage>(),
            provider.GetRequiredService<ActiveJournalSessions>(),
            provider.GetRequiredService<TimeProvider>(),
            AppInfo.GetVersion(),
            provider.GetRequiredService<ILogger<CleanupJournal>>()));
        services.AddSingleton<ICleanupService>(provider => new JournalingCleanupService(
            provider.GetRequiredService<CleanupService>(),
            provider.GetRequiredService<ICleanupJournal>()));
        services.AddSingleton<IHelpSource, EmbeddedHelpSource>();
        services.AddSingleton<HelpCatalog>();

        // Pages, in navigation order.
        services.AddSingleton<ScanPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<ScanPageViewModel>());
        services.AddSingleton<ResultsPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<ResultsPageViewModel>());
        services.AddSingleton<ProgramsPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<ProgramsPageViewModel>());
        services.AddSingleton<FindingsPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<FindingsPageViewModel>());
        services.AddSingleton<PageViewModel>(new PlaceholderPageViewModel(
            "Cleanup", HelpTopics.Cleanup, 3, "Moves the items you select to the Recycle Bin after you confirm."));
        services.AddSingleton<HistoryPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<HistoryPageViewModel>());
        services.AddSingleton<HelpPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<HelpPageViewModel>());
        services.AddSingleton<PageViewModel>(provider => new AboutViewModel(
            AppInfo.GetVersion(), provider.GetRequiredService<AppDataLocation>(), provider.GetRequiredService<IShellLauncher>()));

        // Main window
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}

using LeftoverScout.App.Startup;
using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Logging;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Settings;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;
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
        services.AddSingleton<WindowPlacementService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(ScanTarget.ForCurrentUser());
        services.AddSingleton<IDirectoryReader, Win32DirectoryReader>();
        services.AddSingleton<IPersonalFolderProvider, PersonalFolderProvider>();
        services.AddSingleton<IScanService, ScanService>();

        // Pages, in navigation order.
        services.AddSingleton<ScanPageViewModel>();
        services.AddSingleton<PageViewModel>(provider => provider.GetRequiredService<ScanPageViewModel>());
        services.AddSingleton<PageViewModel>(new PlaceholderPageViewModel(
            "Results", 1, "Lists the folders that look like leftovers and explains why each one is suspected."));
        services.AddSingleton<PageViewModel>(new PlaceholderPageViewModel(
            "Cleanup", 3, "Moves the items you select to the Recycle Bin after you confirm."));
        services.AddSingleton<PageViewModel>(new PlaceholderPageViewModel(
            "History", 3, "Shows what earlier cleanups removed and where it came from."));
        services.AddSingleton<PageViewModel>(provider => new AboutViewModel(
            AppInfo.GetVersion(), provider.GetRequiredService<AppDataLocation>(), provider.GetRequiredService<IShellLauncher>()));

        // Main window
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}

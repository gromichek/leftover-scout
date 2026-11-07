using System.Windows;
using System.Windows.Threading;
using LeftoverScout.App.Composition;
using LeftoverScout.App.Startup;
using LeftoverScout.Core.Logging;
using LeftoverScout.Core.Storage;
using LeftoverScout.Windows.SingleInstance;
using LeftoverScout.Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.App;

/// <summary>
/// Application entry point and composition root: single instance, data folder, logging, dependency
/// injection and global error handling.
/// </summary>
public partial class App : Application
{
    private SingleInstanceGuard? _singleInstance;
    private ServiceProvider? _services;
    private ILogger<App>? _logger;
    private bool _errorDialogOpen;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceGuard();
        if (!_singleInstance.TryAcquire())
        {
            _ = _singleInstance.SignalFirstInstance();
            Shutdown();
            return;
        }

        string programFolder = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataLocation dataLocation = new AppDataLocationResolver(new WritableFolderProbe()).Resolve(programFolder, localAppData);

        LogRetention.Apply(Path.Combine(dataLocation.Path, "logs"));

        _services = ServiceRegistration.Build(dataLocation);
        _logger = _services.GetRequiredService<ILogger<App>>();

        RegisterGlobalErrorHandlers();
        LogStartup(dataLocation);

        var window = _services.GetRequiredService<MainWindow>();
        var placement = _services.GetRequiredService<WindowPlacementService>();
        placement.Restore(window);
        window.Closing += (_, _) => placement.Save(window);

        _singleInstance.StartListening(() => _ = Dispatcher.BeginInvoke(new Action(() => ActivateMainWindow(window))));

        window.Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("LeftoverScout exiting with code {ExitCode}.", e.ApplicationExitCode);
        _singleInstance?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Brings the window to the front when a second instance was started. Relies on the second process having
    /// handed over the foreground right (AllowSetForegroundWindow); <see cref="Window.Activate"/> alone can be
    /// refused by Windows and then only flashes the taskbar button.
    /// </summary>
    private static void ActivateMainWindow(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Show();
        _ = window.Activate();
    }

    private void LogStartup(AppDataLocation dataLocation)
    {
        _logger!.LogInformation(
            "LeftoverScout {Version} starting. OS: {OS}. Data folder: {DataFolder} (fallback: {IsFallback}).",
            AppInfo.GetVersion(),
            Environment.OSVersion,
            dataLocation.Path,
            dataLocation.IsFallback);
    }

    private void RegisterGlobalErrorHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportError(e.Exception, "UI thread");
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ReportError(e.ExceptionObject as Exception, e.IsTerminating ? "Application domain (terminating)" : "Application domain");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportError(e.Exception, "Background task");
        e.SetObserved();
    }

    private void ReportError(Exception? exception, string source)
    {
        _logger?.LogError(exception, "Unhandled exception ({Source}).", source);

        if (_errorDialogOpen)
        {
            return;
        }

        _errorDialogOpen = true;
        try
        {
            _ = MessageBox.Show(
                "Something went wrong. Details were written to the log file in the data folder. If this keeps happening, please report it on the project page.",
                "LeftoverScout",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _errorDialogOpen = false;
        }
    }
}

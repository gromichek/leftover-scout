using System.Windows;
using System.Windows.Media;
using LeftoverScout.Core.Settings;
using LeftoverScout.Core.UI;
using LeftoverScout.Windows.UI;

namespace LeftoverScout.App.Startup;

/// <summary>
/// Restores the main window's bounds from the settings at start and saves them when it closes.
/// </summary>
public sealed class WindowPlacementService
{
    private readonly ISettingsStore _settingsStore;
    private AppSettings _settings;

    /// <summary>Creates the service and loads the current settings.</summary>
    public WindowPlacementService(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _settings = settingsStore.Load();
    }

    /// <summary>
    /// Applies the saved bounds to <paramref name="window"/> if they are visible on a current screen;
    /// otherwise the window is centered on the primary screen. Also writes <c>settings.json</c> so that the
    /// file exists after the first run. Call before the window is shown.
    /// </summary>
    public void Restore(Window window)
    {
        WindowPlacement? saved = _settings.MainWindow;
        IReadOnlyList<ScreenArea> screens = GetScreenAreas(window);

        if (saved is not null && WindowBoundsValidator.IsVisible(saved, screens))
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = saved.Left;
            window.Top = saved.Top;
            window.Width = Math.Max(saved.Width, window.MinWidth);
            window.Height = Math.Max(saved.Height, window.MinHeight);
            if (saved.IsMaximized)
            {
                window.WindowState = WindowState.Maximized;
            }
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        _settingsStore.Save(_settings);
    }

    /// <summary>
    /// Returns the work area of every monitor in device-independent units, so that a window is never restored
    /// into a gap between monitors of different sizes. The pixel areas are converted with the DPI scale of the
    /// window (the system scale before the window is shown). Falls back to the virtual screen if no monitor is reported.
    /// </summary>
    private static IReadOnlyList<ScreenArea> GetScreenAreas(Window window)
    {
        IReadOnlyList<ScreenArea> pixelAreas = MonitorWorkAreas.GetPixelAreas();
        if (pixelAreas.Count == 0)
        {
            return
            [
                new ScreenArea(
                    SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight),
            ];
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(window);
        double scaleX = dpi.DpiScaleX > 0 ? dpi.DpiScaleX : 1;
        double scaleY = dpi.DpiScaleY > 0 ? dpi.DpiScaleY : 1;
        return pixelAreas
            .Select(area => new ScreenArea(area.Left / scaleX, area.Top / scaleY, area.Width / scaleX, area.Height / scaleY))
            .ToList();
    }

    /// <summary>Saves the bounds of <paramref name="window"/>. Call when the window closes.</summary>
    public void Save(Window window)
    {
        // RestoreBounds holds the normal (non-maximized, non-minimized) bounds even when the window is not in that state.
        Rect bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;

        if (bounds.IsEmpty)
        {
            return;
        }

        // Reload first: other parts of the app (for example the Help page) save their own settings in the same file.
        _settings = _settingsStore.Load() with
        {
            MainWindow = new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, window.WindowState == WindowState.Maximized),
        };
        _settingsStore.Save(_settings);
    }
}

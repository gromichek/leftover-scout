using System.Windows;
using LeftoverScout.Core.Settings;
using LeftoverScout.Core.UI;

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
        var screens = new[]
        {
            new ScreenArea(
                SystemParameters.VirtualScreenLeft,
                SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth,
                SystemParameters.VirtualScreenHeight),
        };

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

        _settings = _settings with
        {
            MainWindow = new WindowPlacement(bounds.Left, bounds.Top, bounds.Width, bounds.Height, window.WindowState == WindowState.Maximized),
        };
        _settingsStore.Save(_settings);
    }
}

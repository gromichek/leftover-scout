namespace LeftoverScout.Core.Settings;

/// <summary>
/// Position, size and state of the main window, in device-independent units.
/// </summary>
/// <param name="Left">Left edge.</param>
/// <param name="Top">Top edge.</param>
/// <param name="Width">Width of the restored (non-maximized) window.</param>
/// <param name="Height">Height of the restored (non-maximized) window.</param>
/// <param name="IsMaximized">True when the window was maximized.</param>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height, bool IsMaximized);

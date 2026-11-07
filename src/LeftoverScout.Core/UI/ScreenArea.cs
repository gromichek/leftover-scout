namespace LeftoverScout.Core.UI;

/// <summary>
/// A rectangular area of the desktop in device-independent units.
/// </summary>
/// <param name="Left">Left edge.</param>
/// <param name="Top">Top edge.</param>
/// <param name="Width">Width.</param>
/// <param name="Height">Height.</param>
public sealed record ScreenArea(double Left, double Top, double Width, double Height);

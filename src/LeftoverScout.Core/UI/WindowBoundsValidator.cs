using LeftoverScout.Core.Settings;

namespace LeftoverScout.Core.UI;

/// <summary>
/// Decides whether a saved window position is still usable on the current screens.
/// </summary>
public static class WindowBoundsValidator
{
    /// <summary>Minimum overlap (width) with a screen so that the window can be grabbed and moved.</summary>
    public const double MinimumVisibleWidth = 100;

    /// <summary>Minimum overlap (height) with a screen so that the title bar stays reachable.</summary>
    public const double MinimumVisibleHeight = 50;

    /// <summary>
    /// Returns true when the window overlaps at least one screen enough to be reachable and has a sane size.
    /// </summary>
    /// <param name="placement">The saved placement.</param>
    /// <param name="screens">Currently available screen areas.</param>
    public static bool IsVisible(WindowPlacement placement, IReadOnlyList<ScreenArea> screens)
    {
        if (!IsFinite(placement.Left) || !IsFinite(placement.Top) || !IsFinite(placement.Width) || !IsFinite(placement.Height)
            || placement.Width <= 0 || placement.Height <= 0)
        {
            return false;
        }

        foreach (ScreenArea screen in screens)
        {
            double overlapWidth = Math.Min(placement.Left + placement.Width, screen.Left + screen.Width) - Math.Max(placement.Left, screen.Left);
            double overlapHeight = Math.Min(placement.Top + placement.Height, screen.Top + screen.Height) - Math.Max(placement.Top, screen.Top);
            if (overlapWidth >= MinimumVisibleWidth && overlapHeight >= MinimumVisibleHeight)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

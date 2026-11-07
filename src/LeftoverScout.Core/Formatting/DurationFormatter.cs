using System.Globalization;

namespace LeftoverScout.Core.Formatting;

/// <summary>
/// Formats durations for the status bar and the Scan page ("42 s", "3 min 05 s", "1 h 02 min").
/// </summary>
public static class DurationFormatter
{
    /// <summary>Formats <paramref name="duration"/>, truncated to whole seconds; negative values are 0 s.</summary>
    public static string Format(TimeSpan duration)
    {
        long seconds = Math.Max((long)duration.TotalSeconds, 0);
        if (seconds < 60)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{seconds} s");
        }

        if (seconds < 3600)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{seconds / 60} min {seconds % 60:00} s");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{seconds / 3600} h {seconds % 3600 / 60:00} min");
    }
}

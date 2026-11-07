using System.Globalization;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// Date formatting shared by the Results page.
/// </summary>
internal static class ResultsFormat
{
    /// <summary>Converts a UTC time to local time, short date and short time of the current culture.</summary>
    public static string LocalDateTime(DateTime utc)
    {
        DateTime asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return asUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }

    /// <summary>Converts a moment to local time, short date and short time of the current culture.</summary>
    public static string LocalDateTime(DateTimeOffset moment) =>
        moment.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}

using System.Globalization;

namespace LeftoverScout.Core.Formatting;

/// <summary>
/// Formats byte counts like Windows Explorer: 1 KB = 1024 bytes, one decimal ("1.5 GB").
/// </summary>
public static class SizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    /// <summary>Formats <paramref name="bytes"/>; negative values are shown as 0 B.</summary>
    /// <param name="bytes">Size in bytes.</param>
    /// <param name="culture">Culture for the decimal separator; the current culture when null.</param>
    public static string Format(long bytes, IFormatProvider? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (bytes < 1024)
        {
            return string.Create(culture, $"{Math.Max(bytes, 0)} B");
        }

        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        // Rounding can reach 1024.0 (for example 1023.97 KB); show the next unit instead.
        if (Math.Round(value, 1) >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return string.Create(culture, $"{Math.Round(value, 1):0.0} {Units[unit]}");
    }
}

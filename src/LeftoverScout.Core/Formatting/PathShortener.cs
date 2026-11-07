namespace LeftoverScout.Core.Formatting;

/// <summary>
/// Shortens long paths for display by keeping the end of the path.
/// </summary>
public static class PathShortener
{
    /// <summary>Returns <paramref name="path"/> unchanged if it fits, otherwise "…" plus its last characters.</summary>
    /// <param name="path">The path to shorten.</param>
    /// <param name="maxLength">Maximum result length (at least 2).</param>
    public static string Shorten(string path, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 2);

        return path.Length <= maxLength ? path : "…" + path[^(maxLength - 1)..];
    }
}

namespace LeftoverScout.Core.Cleanup;

/// <summary>The attributes of one path as reported by the file system, without opening it.</summary>
/// <param name="Status">Found, not found or failed.</param>
/// <param name="Attributes">Raw Win32 file attributes; only meaningful when <paramref name="Status"/> is Found.</param>
/// <param name="ReparseTag">The reparse tag; 0 when the path is not a reparse point.</param>
public sealed record PathProbe(PathProbeStatus Status, uint Attributes = 0, uint ReparseTag = 0)
{
    /// <summary>The path does not exist.</summary>
    public static PathProbe NotFound { get; } = new(PathProbeStatus.NotFound);

    /// <summary>The path could not be checked.</summary>
    public static PathProbe Failed { get; } = new(PathProbeStatus.Failed);
}

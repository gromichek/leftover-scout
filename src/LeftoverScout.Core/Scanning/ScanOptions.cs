namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Settings of one scan.
/// </summary>
public sealed record ScanOptions
{
    /// <summary>Default large file threshold: 100 MB (decision D-021).</summary>
    public const long DefaultLargeFileThresholdBytes = 100L * 1024 * 1024;

    /// <summary>Local files of at least this size are listed as large files.</summary>
    public long LargeFileThresholdBytes { get; init; } = DefaultLargeFileThresholdBytes;

    /// <summary>Minimum time between two progress reports.</summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Paths of personal folders; matching folders inside the profile are marked personal.</summary>
    public IReadOnlyList<string> PersonalFolderPaths { get; init; } = [];
}

namespace LeftoverScout.Core.Scanning;

/// <summary>
/// A local file at or above the large file threshold.
/// </summary>
/// <param name="Path">Full path.</param>
/// <param name="Size">Logical size in bytes.</param>
/// <param name="LastWriteTimeUtc">Last write time in UTC.</param>
public sealed record LargeFile(string Path, long Size, DateTime LastWriteTimeUtc);

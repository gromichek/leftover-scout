namespace LeftoverScout.Core.Results;

/// <summary>
/// One row of the Large files table.
/// </summary>
/// <param name="RelativePath">Path relative to the profile root (the full path when outside it).</param>
/// <param name="FullPath">Full path.</param>
/// <param name="Size">Logical size in bytes.</param>
/// <param name="LastWriteTimeUtc">Last write time in UTC.</param>
public sealed record LargeFileRow(string RelativePath, string FullPath, long Size, DateTime LastWriteTimeUtc);

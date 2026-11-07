namespace LeftoverScout.Core.Scanning;

/// <summary>
/// One entry (file or folder) of a directory listing, as reported by the file system without opening it.
/// </summary>
/// <param name="Name">File or folder name without path.</param>
/// <param name="IsDirectory">True for folders.</param>
/// <param name="Length">Logical length in bytes (0 for folders).</param>
/// <param name="LastWriteTimeUtc">Last write time in UTC.</param>
/// <param name="Attributes">Raw Win32 file attributes.</param>
/// <param name="ReparseTag">Reparse tag, 0 when the entry is not a reparse point.</param>
public sealed record DirectoryEntry(
    string Name,
    bool IsDirectory,
    long Length,
    DateTime LastWriteTimeUtc,
    uint Attributes,
    uint ReparseTag);

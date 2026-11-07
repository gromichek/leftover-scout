namespace LeftoverScout.Core.Scanning;

/// <summary>
/// How the scanner treats a directory entry.
/// </summary>
public enum EntryKind
{
    /// <summary>A regular file whose size counts as local size.</summary>
    File,

    /// <summary>A file that is not stored locally (online-only); counted separately.</summary>
    CloudOnlyFile,

    /// <summary>A folder that is scanned.</summary>
    Directory,

    /// <summary>A folder that is not populated locally; it is not enumerated so that nothing is downloaded.</summary>
    CloudFolderNotScanned,

    /// <summary>A junction or symbolic link (name-surrogate reparse point): not followed, not counted.</summary>
    SkippedLink,
}

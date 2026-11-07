namespace LeftoverScout.Core.Results;

/// <summary>
/// Short tags shown in the Notes column of the Results page (decision D-032).
/// </summary>
[Flags]
public enum FolderTags
{
    /// <summary>No tag.</summary>
    None = 0,

    /// <summary>A personal folder (decision D-020).</summary>
    Personal = 1,

    /// <summary>A junction or symbolic link that was not followed.</summary>
    Link = 2,

    /// <summary>A cloud folder that is not available locally and was not read.</summary>
    Cloud = 4,

    /// <summary>A folder that could not be read (access denied or another error).</summary>
    Inaccessible = 8,
}

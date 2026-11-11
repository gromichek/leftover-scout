namespace LeftoverScout.Core.Cleanup;

/// <summary>Result status of removing a whole folder.</summary>
public enum FolderRemoveStatus
{
    /// <summary>Everything was removed.</summary>
    Removed,

    /// <summary>Some entries were removed, some remain.</summary>
    PartlyRemoved,

    /// <summary>Recycle Bin mode: vetoed, nothing was deleted.</summary>
    NotRecycled,

    /// <summary>The folder does not exist.</summary>
    NotFound,

    /// <summary>Nothing could be removed.</summary>
    Failed,
}

namespace LeftoverScout.Core.Cleanup;

/// <summary>Result of removing a single file or folder.</summary>
public enum EntryRemoveStatus
{
    /// <summary>Removed.</summary>
    Removed,

    /// <summary>Did not exist.</summary>
    NotFound,

    /// <summary>Recycle Bin mode: vetoed because Windows could not move it to the Recycle Bin. Nothing was deleted.</summary>
    NotRecycled,

    /// <summary>Could not be removed (in use, access denied, ...).</summary>
    Failed,
}

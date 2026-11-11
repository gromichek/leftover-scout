namespace LeftoverScout.Core.Cleanup;

/// <summary>Why an item was merged into another one.</summary>
public enum MergeReason
{
    /// <summary>The same folder was selected more than once.</summary>
    Duplicate,

    /// <summary>The item lies inside another selected folder, which covers it.</summary>
    InsideSelectedFolder,
}

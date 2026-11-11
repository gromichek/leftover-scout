namespace LeftoverScout.Core.Cleanup;

/// <summary>What happened to one item of a cleanup session.</summary>
public enum CleanupOutcome
{
    /// <summary>The folder was removed completely.</summary>
    Removed,

    /// <summary>Part of the folder was removed; some entries remain (in use, or links that are never followed).</summary>
    PartlyRemoved,

    /// <summary>Recycle Bin mode: the folder is too large for the Recycle Bin, so nothing was done (D-062 layer 1).</summary>
    SkippedTooLargeForRecycleBin,

    /// <summary>Recycle Bin mode: the Recycle Bin is turned off for the drive, so nothing was done (D-062 layer 1).</summary>
    SkippedRecycleBinOff,

    /// <summary>Recycle Bin mode: Windows could not move it to the Recycle Bin and the operation was vetoed (D-062 layer 2).</summary>
    NotRecycled,

    /// <summary>The safety guard blocked the folder; nothing was touched.</summary>
    Blocked,

    /// <summary>The folder does not exist any more.</summary>
    NotFound,

    /// <summary>The folder could not be removed; see the reason.</summary>
    Failed,

    /// <summary>The session was cancelled before this item began.</summary>
    NotStarted,
}

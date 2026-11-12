namespace LeftoverScout.Core.Journal;

/// <summary>
/// What happened to one journal item. The same values as <c>CleanupOutcome</c>, plus <see cref="Pending"/> for an item
/// that was not processed yet (or was never recorded because the session was interrupted).
/// </summary>
public enum JournalOutcome
{
    /// <summary>Not processed yet.</summary>
    Pending,

    /// <summary>The folder was removed completely.</summary>
    Removed,

    /// <summary>Part of the folder was removed; some entries remain.</summary>
    PartlyRemoved,

    /// <summary>Recycle Bin mode: too large for the Recycle Bin, nothing was done.</summary>
    SkippedTooLargeForRecycleBin,

    /// <summary>Recycle Bin mode: the Recycle Bin is off for the drive, nothing was done.</summary>
    SkippedRecycleBinOff,

    /// <summary>Recycle Bin mode: Windows could not move the folder to the Recycle Bin.</summary>
    NotRecycled,

    /// <summary>The safety guard blocked the folder.</summary>
    Blocked,

    /// <summary>The folder did not exist any more.</summary>
    NotFound,

    /// <summary>The folder could not be removed.</summary>
    Failed,

    /// <summary>The session ended before this item began.</summary>
    NotStarted,
}

namespace LeftoverScout.Core.Journal;

/// <summary>The state of a cleanup session as written in its journal file (D-066).</summary>
public enum JournalState
{
    /// <summary>The session has begun and was not finalized yet.</summary>
    InProgress,

    /// <summary>Every item was processed.</summary>
    Completed,

    /// <summary>The user cancelled; the remaining items were not started.</summary>
    Cancelled,

    /// <summary>The session stopped because of an unexpected error.</summary>
    Failed,
}

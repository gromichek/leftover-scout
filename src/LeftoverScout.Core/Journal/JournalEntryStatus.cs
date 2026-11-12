namespace LeftoverScout.Core.Journal;

/// <summary>How a journal file is shown in the History list.</summary>
public enum JournalEntryStatus
{
    /// <summary>Finished normally.</summary>
    Completed,

    /// <summary>The user cancelled the session.</summary>
    Cancelled,

    /// <summary>The session stopped because of an error.</summary>
    Failed,

    /// <summary>The session is running in this process right now.</summary>
    InProgress,

    /// <summary>The file says "in progress", but no session of this process is running: it was cut off.</summary>
    Interrupted,

    /// <summary>The file could not be read or is not understood.</summary>
    Unreadable,
}

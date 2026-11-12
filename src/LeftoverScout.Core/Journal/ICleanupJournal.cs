using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Journal;

/// <summary>
/// Records a cleanup session on disk while it runs (D-066). No method throws: a failed write sets
/// <see cref="JournalHandle.Warning"/> and the cleanup goes on.
/// </summary>
public interface ICleanupJournal
{
    /// <summary>Raised (possibly on a background thread) when a session began or ended, so pages can refresh.</summary>
    event EventHandler? SessionRecorded;

    /// <summary>Creates the journal file before the first item; every item is listed as pending.</summary>
    /// <param name="request">The request; it is reduced the same way the cleanup service reduces it.</param>
    /// <param name="mode">The cleanup mode.</param>
    JournalHandle Begin(CleanupRequest request, CleanupMode mode);

    /// <summary>Records the result of one item and rewrites the file atomically.</summary>
    void Record(JournalHandle handle, CleanupItemResult result);

    /// <summary>Finalizes the journal with the result of the session (state Completed, or Cancelled when items did not start).</summary>
    void Complete(JournalHandle handle, CleanupSessionResult session);

    /// <summary>
    /// Finalizes the journal of a session that ended without a result. Items that were not recorded become
    /// <see cref="JournalOutcome.NotStarted"/>.
    /// </summary>
    /// <param name="handle">The session.</param>
    /// <param name="state"><see cref="JournalState.Cancelled"/> or <see cref="JournalState.Failed"/>.</param>
    void Abort(JournalHandle handle, JournalState state);
}

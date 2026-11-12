namespace LeftoverScout.Core.Cleanup;

/// <summary>The result of a whole cleanup session.</summary>
/// <param name="StartedUtc">Start time (UTC).</param>
/// <param name="EndedUtc">End time (UTC).</param>
/// <param name="Mode">The mode that was asked for.</param>
/// <param name="Items">One result per item, in order; items that did not start are listed as not started.</param>
public sealed record CleanupSessionResult(
    DateTime StartedUtc,
    DateTime EndedUtc,
    CleanupMode Mode,
    IReadOnlyList<CleanupItemResult> Items)
{
    /// <summary>
    /// A short English message when the journal of this session could not be written completely (the cleanup itself
    /// was not affected); null otherwise. Set by the journaling decorator.
    /// </summary>
    public string? JournalWarning { get; init; }

    /// <summary>Total bytes removed.</summary>
    public long BytesRemoved => Items.Sum(item => item.BytesRemoved);

    /// <summary>True when any item was deleted permanently although the Recycle Bin was asked for.</summary>
    public bool DeletedPermanentlyUnexpectedly => Items.Any(item => item.DeletedPermanentlyUnexpectedly);

    /// <summary>True when the session stopped before every item began.</summary>
    public bool WasCancelled => Items.Any(item => item.Outcome == CleanupOutcome.NotStarted);

    /// <summary>Number of items with the given outcome.</summary>
    public int Count(CleanupOutcome outcome) => Items.Count(item => item.Outcome == outcome);
}

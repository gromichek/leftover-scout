namespace LeftoverScout.Core.Cleanup;

/// <summary>What the Cleanup page shows while the user reviews a request before starting.</summary>
/// <param name="Rows">One row for each item that remains after the reduction, in order.</param>
/// <param name="MergedCount">Number of selected items that lie inside (or repeat) another selected item and go with it.</param>
/// <param name="RunningPrograms">Names of the running programs that seem to belong to ready items; distinct and sorted.</param>
public sealed record CleanupReview(
    IReadOnlyList<CleanupReviewRow> Rows,
    int MergedCount,
    IReadOnlyList<string> RunningPrograms)
{
    /// <summary>The ready items, in order. Only these are passed to the cleanup service.</summary>
    public IReadOnlyList<CleanupItem> ReadyItems => [.. Rows.Where(row => row.IsReady).Select(row => row.Item)];

    /// <summary>Number of ready items.</summary>
    public int ReadyCount => Rows.Count(row => row.IsReady);

    /// <summary>Number of blocked items.</summary>
    public int BlockedCount => Rows.Count - ReadyCount;

    /// <summary>Local bytes of the ready items.</summary>
    public long ReadySize => Rows.Where(row => row.IsReady).Sum(row => row.Item.SizeBytes);

    /// <summary>Number of ready items with Low confidence.</summary>
    public int ReadyLowConfidenceCount =>
        Rows.Count(row => row.IsReady && row.Item.Confidence == Classification.LeftoverConfidence.Low);
}

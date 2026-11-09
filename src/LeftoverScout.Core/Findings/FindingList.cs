namespace LeftoverScout.Core.Findings;

/// <summary>The visible findings with their total (nested items are not counted twice).</summary>
/// <param name="Entries">The visible findings.</param>
/// <param name="TotalSize">Local bytes of the visible findings, without items inside another visible finding.</param>
public sealed record FindingList(IReadOnlyList<FindingEntry> Entries, long TotalSize)
{
    /// <summary>Number of visible items (nested ones included).</summary>
    public int Count => Entries.Count;
}

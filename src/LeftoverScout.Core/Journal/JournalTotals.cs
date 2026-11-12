namespace LeftoverScout.Core.Journal;

/// <summary>Totals of a journal. They are recomputed from the items whenever the file is written.</summary>
public sealed class JournalTotals
{
    /// <summary>Number of items in the session.</summary>
    public int ItemCount { get; set; }

    /// <summary>Bytes removed in total.</summary>
    public long BytesRemoved { get; set; }

    /// <summary>Number of items per outcome (outcomes without items are left out).</summary>
    public Dictionary<string, int> Outcomes { get; set; } = [];
}

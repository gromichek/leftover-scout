using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Findings;

namespace LeftoverScout.Core.Journal;

/// <summary>One folder of a cleanup session as written in the journal. Contains real paths: never log it.</summary>
public sealed class JournalItem
{
    /// <summary>Full path of the folder.</summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>Path relative to the profile root.</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>Leftover, cache, temp, logs or crash dumps.</summary>
    public FindingKind Kind { get; init; }

    /// <summary>The confidence of a leftover; null for cache kinds.</summary>
    public LeftoverConfidence? Confidence { get; init; }

    /// <summary>Logical size in bytes as seen by the scan.</summary>
    public long SizeAtScan { get; init; }

    /// <summary>What happened; <see cref="JournalOutcome.Pending"/> until the item was recorded.</summary>
    public JournalOutcome Outcome { get; set; } = JournalOutcome.Pending;

    /// <summary>A short English reason for every outcome other than a complete removal.</summary>
    public string? Reason { get; set; }

    /// <summary>For a partly removed folder: how many entries remain.</summary>
    public int ItemsLeft { get; set; }

    /// <summary>Bytes removed.</summary>
    public long BytesRemoved { get; set; }

    /// <summary>True when Windows deleted the folder permanently although the Recycle Bin was asked for (D-062).</summary>
    public bool DeletedPermanentlyUnexpectedly { get; set; }
}

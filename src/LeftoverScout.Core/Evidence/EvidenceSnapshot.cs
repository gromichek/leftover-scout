namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Everything the evidence collector found in one run.
/// </summary>
/// <param name="Programs">All entries from all sources that worked.</param>
/// <param name="SourceCounts">Number of entries per source (0 for a failed source).</param>
/// <param name="Errors">Sources that failed.</param>
/// <param name="CollectedAt">When the collection finished.</param>
/// <param name="Duration">How long the collection took.</param>
public sealed record EvidenceSnapshot(
    IReadOnlyList<InstalledProgram> Programs,
    IReadOnlyDictionary<ProgramSource, int> SourceCounts,
    IReadOnlyList<EvidenceError> Errors,
    DateTimeOffset CollectedAt,
    TimeSpan Duration);

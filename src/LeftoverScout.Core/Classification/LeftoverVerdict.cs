namespace LeftoverScout.Core.Classification;

/// <summary>
/// The result of leftover detection for one folder (decisions D-046, D-047).
/// </summary>
/// <param name="Kind">In use, leftover or unknown.</param>
/// <param name="Confidence">The confidence; set only when <paramref name="Kind"/> is <see cref="LeftoverVerdictKind.Leftover"/>.</param>
/// <param name="ProgramName">The installed program the folder belongs to; set only for <see cref="LeftoverVerdictKind.InUse"/>.</param>
/// <param name="Explanation">A short English explanation shown as tooltip.</param>
public sealed record LeftoverVerdict(
    LeftoverVerdictKind Kind,
    LeftoverConfidence? Confidence,
    string? ProgramName,
    string Explanation)
{
    /// <summary>Creates an "in use" verdict.</summary>
    public static LeftoverVerdict InUse(string programName, string explanation) =>
        new(LeftoverVerdictKind.InUse, null, programName, explanation);

    /// <summary>Creates a leftover verdict.</summary>
    public static LeftoverVerdict Leftover(LeftoverConfidence confidence, string explanation) =>
        new(LeftoverVerdictKind.Leftover, confidence, null, explanation);

    /// <summary>Creates an "unknown" verdict.</summary>
    public static LeftoverVerdict Unknown(string explanation) =>
        new(LeftoverVerdictKind.Unknown, null, null, explanation);

    /// <summary>
    /// The Notes label: <c>Leftover: High</c>, <c>Leftover: Medium</c>, <c>Leftover: Low</c> or <c>In use: Program</c>.
    /// Empty for an unknown verdict.
    /// </summary>
    public string Label => Kind switch
    {
        LeftoverVerdictKind.Leftover => $"Leftover: {Confidence}",
        LeftoverVerdictKind.InUse => $"In use: {ProgramName}",
        _ => string.Empty,
    };
}

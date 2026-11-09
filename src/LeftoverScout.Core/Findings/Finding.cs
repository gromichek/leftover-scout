using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Findings;

/// <summary>
/// One item of the Findings page: a leftover candidate or a cache / temp / logs / crash dump folder (decision D-053).
/// </summary>
/// <param name="Node">The folder in the scan tree.</param>
/// <param name="RelativePath">Path below the profile root.</param>
/// <param name="Kind">Leftover, cache, temp, logs or crash dumps.</param>
/// <param name="Confidence">The confidence; set for leftovers only.</param>
/// <param name="ProgramName">The program the folder belongs to; null when unknown.</param>
/// <param name="Explanation">Explanation lines shown in the details panel.</param>
/// <param name="RuleId">Id of the matched rule; null when no rule matched.</param>
/// <param name="RuleDescription">Description of the matched rule; null when no rule matched.</param>
public sealed record Finding(
    FolderNode Node,
    string RelativePath,
    FindingKind Kind,
    LeftoverConfidence? Confidence,
    string? ProgramName,
    IReadOnlyList<string> Explanation,
    string? RuleId,
    string? RuleDescription)
{
    /// <summary>Full path of the folder.</summary>
    public string FullPath => Node.FullPath;

    /// <summary>Logical bytes of local files in the folder.</summary>
    public long LocalSize => Node.LocalSize;

    /// <summary>Number of files in the folder.</summary>
    public long FileCount => Node.FileCount;

    /// <summary>Newest write time (UTC) in the folder; null when there are no files.</summary>
    public DateTime? NewestWriteTimeUtc => Node.NewestWriteTimeUtc;

    /// <summary>The kind as shown in the table: <c>Leftover</c>, <c>Cache</c>, <c>Temp</c>, <c>Logs</c> or <c>Crash dumps</c>.</summary>
    public string KindText => Kind switch
    {
        FindingKind.Leftover => "Leftover",
        FindingKind.Cache => "Cache",
        FindingKind.Temp => "Temp",
        FindingKind.Logs => "Logs",
        _ => "Crash dumps",
    };

    /// <summary>The confidence as shown in the table; empty for cache kinds.</summary>
    public string ConfidenceText => Confidence?.ToString() ?? string.Empty;
}

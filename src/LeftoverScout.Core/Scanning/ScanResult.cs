using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;

namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Outcome of a scan.
/// </summary>
/// <param name="Completion">How the scan ended.</param>
/// <param name="Root">Folder tree of the profile; null when the scan was cancelled.</param>
/// <param name="LargeFiles">Local files at or above the threshold, largest first.</param>
/// <param name="Errors">Locations that could not be read.</param>
/// <param name="SkippedLinkCount">Junctions and symbolic links that were not followed.</param>
/// <param name="CloudFoldersNotScannedCount">Cloud folders that were not populated locally and not enumerated.</param>
/// <param name="PersonalLocalSize">Local bytes inside personal folders (nested personal folders counted once).</param>
/// <param name="Duration">How long the scan took.</param>
public sealed record ScanResult(
    ScanCompletion Completion,
    FolderNode? Root,
    IReadOnlyList<LargeFile> LargeFiles,
    IReadOnlyList<ScanError> Errors,
    int SkippedLinkCount,
    int CloudFoldersNotScannedCount,
    long PersonalLocalSize,
    TimeSpan Duration)
{
    /// <summary>True when the scan ran to the end.</summary>
    public bool IsCompleted => Completion == ScanCompletion.Completed;

    /// <summary>When the scan finished (set by the scan service for completed scans); null otherwise.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Installed programs collected together with this scan (set by the scan service for completed scans).</summary>
    public EvidenceSnapshot? Evidence { get; init; }

    /// <summary>
    /// The rules that matched the folders of this scan (ownership, caches, protected locations). Computed once by the
    /// scan service after the scan; null when matching was not done or failed.
    /// </summary>
    public RuleMatchMap? RuleMatches { get; init; }

    /// <summary>
    /// The leftover verdicts (decisions D-044 … D-047). Computed once by the scan service after rule matching; null when
    /// classification was not done or failed.
    /// </summary>
    public LeftoverMap? Leftovers { get; init; }
}

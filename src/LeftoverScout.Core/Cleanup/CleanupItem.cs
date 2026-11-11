using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Findings;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// One folder the user selected for cleanup. Size, newest write time and confidence are what the scan saw;
/// the guard uses them for warnings only and never trusts them for a decision.
/// </summary>
/// <param name="FullPath">Full path of the folder.</param>
/// <param name="Kind">Leftover, cache, temp, logs or crash dumps.</param>
/// <param name="Confidence">The confidence of a leftover; null for cache kinds.</param>
/// <param name="SizeBytes">Logical size in bytes as seen by the scan.</param>
/// <param name="NewestWriteTimeUtc">Newest write time (UTC) in the folder as seen by the scan; null when it had no files.</param>
public sealed record CleanupItem(
    string FullPath,
    FindingKind Kind,
    LeftoverConfidence? Confidence,
    long SizeBytes,
    DateTime? NewestWriteTimeUtc);

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
}

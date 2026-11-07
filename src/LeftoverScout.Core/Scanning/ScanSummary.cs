namespace LeftoverScout.Core.Scanning;

/// <summary>
/// The numbers shown on the Scan page after a completed scan.
/// </summary>
/// <param name="LocalSize">Local bytes in the profile.</param>
/// <param name="CloudOnlySize">Bytes of cloud-only files (not part of the local size).</param>
/// <param name="PersonalSize">Local bytes inside personal folders.</param>
/// <param name="FolderCount">Number of folders.</param>
/// <param name="FileCount">Number of files.</param>
/// <param name="SkippedLinkCount">Junctions and symbolic links not followed.</param>
/// <param name="CloudFoldersNotScannedCount">Cloud folders not populated locally.</param>
/// <param name="InaccessibleCount">Locations that could not be read.</param>
/// <param name="Duration">Scan duration.</param>
/// <param name="LargestTopLevelFolders">The largest top-level folders of the profile.</param>
public sealed record ScanSummary(
    long LocalSize,
    long CloudOnlySize,
    long PersonalSize,
    long FolderCount,
    long FileCount,
    int SkippedLinkCount,
    int CloudFoldersNotScannedCount,
    int InaccessibleCount,
    TimeSpan Duration,
    IReadOnlyList<TopFolder> LargestTopLevelFolders)
{
    /// <summary>Number of top-level folders listed.</summary>
    public const int TopFolderCount = 10;

    /// <summary>Builds the summary of a completed scan.</summary>
    /// <exception cref="ArgumentException">The scan was cancelled and has no totals.</exception>
    public static ScanSummary From(ScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Root is not { } root)
        {
            throw new ArgumentException("A cancelled scan has no totals.", nameof(result));
        }

        List<TopFolder> top = root.Children
            .Where(child => !child.IsSkippedLink)
            .OrderByDescending(child => child.LocalSize)
            .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .Take(TopFolderCount)
            .Select(child => new TopFolder(child.Name, child.LocalSize))
            .ToList();

        return new ScanSummary(
            root.LocalSize,
            root.CloudOnlySize,
            result.PersonalLocalSize,
            root.SubfolderCount,
            root.FileCount,
            result.SkippedLinkCount,
            result.CloudFoldersNotScannedCount,
            result.Errors.Count,
            result.Duration,
            top);
    }
}

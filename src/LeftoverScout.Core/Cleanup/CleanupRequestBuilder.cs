using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Cleanup;

/// <summary>Builds cleanup requests from the findings the user selected, and measures a selection.</summary>
public static class CleanupRequestBuilder
{
    /// <summary>Creates the request for the selected findings. The guard and the service re-check every item.</summary>
    public static CleanupRequest FromFindings(ScanTarget target, IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(findings);

        List<CleanupItem> items = [.. findings.Select(ToItem)];
        return new CleanupRequest(target, items);
    }

    /// <summary>
    /// Local bytes of the selected findings without double counting: an item inside another selected item is covered by
    /// the outer one (<see cref="RequestReducer"/>).
    /// </summary>
    public static long SelectedSize(IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        RequestReduction reduction = RequestReducer.Reduce([.. findings.Select(ToItem)]);
        return reduction.Items.Sum(item => item.SizeBytes);
    }

    private static CleanupItem ToItem(Finding finding) =>
        new(finding.FullPath, finding.Kind, finding.Confidence, finding.LocalSize, finding.NewestWriteTimeUtc, finding.ProgramName);
}

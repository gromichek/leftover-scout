using LeftoverScout.Core.Findings;

namespace LeftoverScout.Core.Cleanup;

/// <summary>Turns the guard report and the running-program warnings into the review of the Cleanup page. Pure.</summary>
public static class CleanupReviewBuilder
{
    /// <summary>Builds the review. Warnings of running programs are collected for ready items only.</summary>
    /// <param name="request">The request that was checked.</param>
    /// <param name="report">The guard report for <paramref name="request"/>.</param>
    /// <param name="running">The running-program warnings (may be empty).</param>
    public static CleanupReview Build(
        CleanupRequest request,
        CleanupGuardReport report,
        IReadOnlyList<RunningProgramWarning> running)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(running);

        var rows = new List<CleanupReviewRow>(report.Items.Count);
        foreach (GuardItemResult entry in report.Items)
        {
            CleanupItem item = entry.Item;
            GuardResult result = entry.Result;
            string status = result.IsAllowed
                ? ReadyText(result.Warnings)
                : "Blocked: " + string.Join(" ", result.BlockReasons.Select(reason => reason.Text));
            rows.Add(new CleanupReviewRow(
                item,
                CleanupPaths.Relative(request.Target.ProfileRootPath, item.FullPath),
                KindText(item.Kind),
                item.Confidence?.ToString() ?? string.Empty,
                result.IsAllowed,
                status));
        }

        var readyPaths = new HashSet<string>(
            rows.Where(row => row.IsReady).Select(row => row.Item.FullPath),
            StringComparer.OrdinalIgnoreCase);
        string[] programs =
        [
            .. running
                .Where(warning => readyPaths.Contains(warning.Item.FullPath))
                .SelectMany(warning => warning.ProcessNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase),
        ];

        return new CleanupReview(rows, report.Reduction.Merged.Count, programs);
    }

    private static string ReadyText(IReadOnlyList<GuardWarning> warnings)
    {
        if (warnings.Count == 0)
        {
            return "Ready";
        }

        // Short labels; the guard texts are sentences.
        IEnumerable<string> labels = warnings.Select(warning => warning.Code switch
        {
            GuardWarningCode.ChangedSinceScan => "Changed since the scan",
            GuardWarningCode.LowConfidence => "Low confidence",
            _ => "Medium confidence",
        });
        return "Ready · " + string.Join(" · ", labels);
    }

    private static string KindText(FindingKind kind) => kind switch
    {
        FindingKind.Leftover => "Leftover",
        FindingKind.Cache => "Cache",
        FindingKind.Temp => "Temp",
        FindingKind.Logs => "Logs",
        _ => "Crash dumps",
    };
}

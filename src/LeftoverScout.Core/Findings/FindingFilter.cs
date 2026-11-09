using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Findings;

/// <summary>
/// The filter of the Findings page (decision D-053): which confidences and cache kinds are shown, plus a search text.
/// </summary>
/// <param name="ShowHigh">Show leftovers with High confidence.</param>
/// <param name="ShowMedium">Show leftovers with Medium confidence.</param>
/// <param name="ShowLow">Show leftovers with Low confidence.</param>
/// <param name="ShowCache">Show cache folders.</param>
/// <param name="ShowTemp">Show temp folders.</param>
/// <param name="ShowLogs">Show log folders.</param>
/// <param name="ShowCrashDumps">Show crash dump folders.</param>
/// <param name="SearchText">Case-insensitive text that must occur in the path or the program name; empty shows everything.</param>
public sealed record FindingFilter(
    bool ShowHigh = true,
    bool ShowMedium = true,
    bool ShowLow = false,
    bool ShowCache = true,
    bool ShowTemp = true,
    bool ShowLogs = true,
    bool ShowCrashDumps = true,
    string SearchText = "")
{
    /// <summary>The default filter: High, Medium and all cache kinds; Low is hidden.</summary>
    public static FindingFilter Default { get; } = new();

    /// <summary>True when <paramref name="finding"/> passes the filter.</summary>
    public bool Matches(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        bool kindShown = finding.Kind switch
        {
            FindingKind.Leftover => finding.Confidence switch
            {
                LeftoverConfidence.High => ShowHigh,
                LeftoverConfidence.Medium => ShowMedium,
                _ => ShowLow,
            },
            FindingKind.Cache => ShowCache,
            FindingKind.Temp => ShowTemp,
            FindingKind.Logs => ShowLogs,
            _ => ShowCrashDumps,
        };

        if (!kindShown)
        {
            return false;
        }

        string text = SearchText.Trim();
        return text.Length == 0
            || finding.RelativePath.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (finding.ProgramName?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    /// <summary>
    /// Applies the filter and works out the totals. An item that lies inside another <em>visible</em> item is kept in
    /// the list but not counted in the size, so nothing is counted twice.
    /// </summary>
    public FindingList Apply(IReadOnlyList<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        List<Finding> visible = findings.Where(Matches).ToList();
        var byNode = new Dictionary<FolderNode, Finding>();
        foreach (Finding finding in visible)
        {
            byNode[finding.Node] = finding;
        }

        var entries = new List<FindingEntry>(visible.Count);
        long total = 0;
        foreach (Finding finding in visible)
        {
            Finding? outer = null;
            for (FolderNode? ancestor = finding.Node.Parent; ancestor is not null; ancestor = ancestor.Parent)
            {
                if (byNode.TryGetValue(ancestor, out Finding? found))
                {
                    outer = found;
                    break;
                }
            }

            entries.Add(new FindingEntry(finding, outer));
            if (outer is null)
            {
                total += finding.LocalSize;
            }
        }

        return new FindingList(entries, total);
    }
}

using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Findings;

/// <summary>
/// Builds the findings of a completed scan (decisions D-052 … D-055): every leftover verdict plus the top-most
/// cache / temp / logs / crash dump folders. The builder re-checks protection itself; it does not rely on the classifier.
/// </summary>
public static class FindingBuilder
{
    /// <summary>
    /// Builds the findings, in no particular order. Returns an empty list when the scan has no folder tree.
    /// Never included: personal or protected folders (also when an ancestor is personal or protected with subtree scope),
    /// skipped links, cloud folders that were not scanned, and folders without local data (no size and no files).
    /// </summary>
    public static IReadOnlyList<Finding> Build(ScanResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.Root is not { } root)
        {
            return [];
        }

        RuleMatchMap? rules = result.RuleMatches;
        var findings = new List<Finding>();
        var leftoverNodes = new HashSet<FolderNode>();

        if (result.Leftovers is { } leftovers)
        {
            foreach ((FolderNode node, LeftoverVerdict verdict) in leftovers.Entries)
            {
                if (verdict.Kind != LeftoverVerdictKind.Leftover || verdict.Confidence is not { } confidence)
                {
                    continue;
                }

                if (!IsEligible(node, root, rules))
                {
                    continue;
                }

                leftoverNodes.Add(node);
                findings.Add(CreateLeftover(node, root, rules?.Get(node), verdict, confidence));
            }
        }

        if (rules is not null)
        {
            AddCaches(root, root, rules, leftoverNodes, findings);
        }

        return findings;
    }

    private static void AddCaches(FolderNode folder, FolderNode root, RuleMatchMap rules, HashSet<FolderNode> leftoverNodes, List<Finding> findings)
    {
        // Explicit stack: the tree can be deep. The flag says that an ancestor is already listed as a cache.
        var stack = new Stack<(FolderNode Node, bool InsideListedCache)>();
        stack.Push((folder, false));

        while (stack.Count > 0)
        {
            (FolderNode node, bool inside) = stack.Pop();
            bool listedAsCache = false; // true when this folder (or its leftover entry) already covers its subtree

            if (node.Parent is not null
                && !inside
                && !leftoverNodes.Contains(node)
                && rules.Get(node) is { Primary.Rule.Kind: RuleKind.Cache } match
                && IsEligible(node, root, rules))
            {
                findings.Add(CreateCache(node, root, match));
                listedAsCache = true;
            }
            else if (!inside && leftoverNodes.Contains(node) && rules.Get(node) is { Primary.Rule.Kind: RuleKind.Cache })
            {
                // Listed once, as a leftover (its explanation mentions the cache rule). What lies below it is part of it.
                listedAsCache = true;
            }

            foreach (FolderNode child in node.Children)
            {
                stack.Push((child, inside || listedAsCache));
            }
        }
    }

    /// <summary>Safety checks of a folder and its ancestors; protection is absolute.</summary>
    private static bool IsEligible(FolderNode node, FolderNode root, RuleMatchMap? rules)
    {
        if (node.Parent is null || ReferenceEquals(node, root))
        {
            return false;
        }

        if (node.IsPersonal || node.IsSkippedLink || node.IsCloudFolderNotScanned)
        {
            return false;
        }

        if (node.LocalSize <= 0 && node.FileCount <= 0)
        {
            return false;
        }

        if (rules?.Get(node) is { IsProtected: true })
        {
            return false;
        }

        for (FolderNode? ancestor = node.Parent; ancestor is not null; ancestor = ancestor.Parent)
        {
            if (ancestor.IsPersonal || ancestor.IsSkippedLink || ancestor.IsCloudFolderNotScanned)
            {
                return false;
            }

            // Folder-scope protection (for example AppData\Local itself) does not reach the children.
            if (rules?.Get(ancestor) is { } ancestorMatch
                && ancestorMatch.All.Any(match => match.Rule.Kind == RuleKind.Protected && match.Rule.Scope == RuleScope.Subtree))
            {
                return false;
            }
        }

        return true;
    }

    private static Finding CreateLeftover(FolderNode node, FolderNode root, FolderRuleMatch? match, LeftoverVerdict verdict, LeftoverConfidence confidence)
    {
        var lines = new List<string> { verdict.Explanation };
        if (match is not null)
        {
            foreach (RuleMatch ruleMatch in match.All.Where(item => !item.IsPersonalFolder))
            {
                string prefix = ruleMatch.Rule.Kind == RuleKind.Cache ? "It also matches a cache rule: " : "Rule: ";
                lines.Add(prefix + RuleText(ruleMatch));
            }
        }

        RuleMatch? primary = match?.All.FirstOrDefault(item => !item.IsPersonalFolder);
        string? program = match?.All
            .Where(item => item.Rule.Program is not null)
            .Select(item => item.Rule.Program!.DisplayName)
            .FirstOrDefault();

        return new Finding(node, RelativePath(node, root), FindingKind.Leftover, confidence, program, lines, primary?.Rule.Id, primary?.Rule.Description);
    }

    private static Finding CreateCache(FolderNode node, FolderNode root, FolderRuleMatch match)
    {
        Rule rule = match.Primary.Rule;
        (FindingKind kind, string sentence) = rule.Category switch
        {
            CacheCategory.Temp => (FindingKind.Temp, "Temporary files that programs can recreate."),
            CacheCategory.Logs => (FindingKind.Logs, "Log files."),
            CacheCategory.CrashDumps => (FindingKind.CrashDumps, "Files that Windows or a program wrote after a failure."),
            _ => (FindingKind.Cache, "Data that a program can download or build again."),
        };

        var lines = new List<string> { sentence };
        foreach (RuleMatch ruleMatch in match.All.Where(item => !item.IsPersonalFolder))
        {
            lines.Add("Rule: " + RuleText(ruleMatch));
        }

        return new Finding(node, RelativePath(node, root), kind, null, rule.Program?.DisplayName, lines, rule.Id, rule.Description);
    }

    private static string RuleText(RuleMatch match) =>
        match.IsInherited ? $"{match.Rule.Description} (rule of a parent folder)" : match.Rule.Description;

    private static string RelativePath(FolderNode node, FolderNode root)
    {
        var names = new List<string>();
        for (FolderNode? current = node; current is not null && !ReferenceEquals(current, root); current = current.Parent)
        {
            names.Add(current.Name);
        }

        names.Reverse();
        return string.Join('\\', names);
    }
}

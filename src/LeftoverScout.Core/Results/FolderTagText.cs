using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// Converts the flags of a <see cref="FolderNode"/> into Notes tags and their texts.
/// </summary>
public static class FolderTagText
{
    private static readonly (FolderTags Tag, string Label, string Explanation)[] Definitions =
    [
        (FolderTags.Personal, "Personal", "Personal: one of your own folders (Documents, Desktop, Pictures, Videos, Music, Downloads or OneDrive). It is measured, and it is protected: it is never offered for cleanup."),
        (FolderTags.Link, "Link", "Link: a junction or symbolic link. It is not followed and not counted, so nothing is counted twice."),
        (FolderTags.Cloud, "Cloud", "Cloud: an online-only OneDrive folder that is not available on this computer. It was not read, so that nothing is downloaded."),
        (FolderTags.Inaccessible, "Inaccessible", "Inaccessible: the folder could not be read completely (access denied or another error). Its size may be too small."),
    ];

    private const string ProtectedExplanation = "Protected: LeftoverScout never offers this folder for deletion.";

    /// <summary>Returns the tags that apply to <paramref name="node"/>.</summary>
    public static FolderTags FromNode(FolderNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        FolderTags tags = FolderTags.None;
        if (node.IsPersonal)
        {
            tags |= FolderTags.Personal;
        }

        if (node.IsSkippedLink)
        {
            tags |= FolderTags.Link;
        }

        if (node.IsCloudFolderNotScanned)
        {
            tags |= FolderTags.Cloud;
        }

        if (node.IsAccessDenied || node.HasError)
        {
            tags |= FolderTags.Inaccessible;
        }

        return tags;
    }

    /// <summary>The tag names joined with ", " (empty when there is no tag).</summary>
    public static string Describe(FolderTags tags) =>
        string.Join(", ", Definitions.Where(definition => tags.HasFlag(definition.Tag)).Select(definition => definition.Label));

    /// <summary>One sentence per tag, one tag per line (empty when there is no tag). Used as tooltip.</summary>
    public static string Explain(FolderTags tags) =>
        string.Join("\n", Definitions.Where(definition => tags.HasFlag(definition.Tag)).Select(definition => definition.Explanation));

    /// <summary>
    /// The rule label of a folder (decision D-043): <c>Protected</c>, <c>Cache: Discord</c>, <c>Temp</c>, <c>Logs</c>,
    /// <c>Crash dumps</c> or <c>Owner: Discord</c>. Empty when no rule applies. Personal folders have no rule label;
    /// they show the <c>Personal</c> tag.
    /// </summary>
    public static string RuleLabel(FolderRuleMatch? match)
    {
        if (match is null || match.Primary.IsPersonalFolder)
        {
            return string.Empty;
        }

        Rule rule = match.Primary.Rule;
        return rule.Kind switch
        {
            RuleKind.Protected => "Protected",
            RuleKind.Ownership => $"Owner: {rule.Program?.DisplayName ?? rule.Id}",
            _ => rule.Category switch
            {
                CacheCategory.Temp => "Temp",
                CacheCategory.Logs => "Logs",
                CacheCategory.CrashDumps => "Crash dumps",
                _ => rule.Program is { } program ? $"Cache: {program.DisplayName}" : "Cache",
            },
        };
    }

    /// <summary>
    /// The tags, the rule label and the leftover label (<c>Leftover: High</c>, <c>In use: Discord</c>; decision D-047)
    /// joined with ", " (empty when there is nothing to show).
    /// </summary>
    public static string Describe(FolderTags tags, FolderRuleMatch? match, LeftoverVerdict? verdict = null)
    {
        var parts = new List<string>(3);
        AddIfNotEmpty(parts, Describe(tags));
        AddIfNotEmpty(parts, RuleLabel(match));
        AddIfNotEmpty(parts, verdict?.Label);
        return string.Join(", ", parts);
    }

    private static void AddIfNotEmpty(List<string> parts, string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            parts.Add(text);
        }
    }

    /// <summary>
    /// The tooltip: one sentence per tag, then the explanation of the rule label and the description of every
    /// matched rule, the primary rule first (empty when there is nothing to show).
    /// </summary>
    public static string Explain(FolderTags tags, FolderRuleMatch? match, LeftoverVerdict? verdict = null)
    {
        var lines = new List<string>();
        string tagText = Explain(tags);
        if (tagText.Length > 0)
        {
            lines.Add(tagText);
        }

        if (match is not null)
        {
            string label = RuleLabel(match);
            if (label.Length > 0)
            {
                lines.Add(ExplainLabel(match.Primary.Rule));
            }

            foreach (RuleMatch ruleMatch in match.All.Where(item => !item.IsPersonalFolder))
            {
                lines.Add(ruleMatch.IsInherited
                    ? $"• {ruleMatch.Rule.Description} (rule of a parent folder)"
                    : $"• {ruleMatch.Rule.Description}");
            }
        }

        if (verdict is { Kind: not LeftoverVerdictKind.Unknown })
        {
            lines.Add(verdict.Explanation);
        }

        return string.Join("\n", lines);
    }

    private static string ExplainLabel(Rule rule) => rule.Kind switch
    {
        RuleKind.Protected => ProtectedExplanation,
        RuleKind.Ownership => "Owner: this folder belongs to the program named in the tag.",
        _ => rule.Category switch
        {
            CacheCategory.Temp => "Temp: temporary files that programs can recreate.",
            CacheCategory.Logs => "Logs: log files.",
            CacheCategory.CrashDumps => "Crash dumps: files that Windows or a program wrote after a failure.",
            _ => "Cache: data that a program can download or build again.",
        },
    };
}

using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Classification;

/// <summary>
/// Chooses the folders that leftover detection looks at (decision D-044). Pure; works on the scan tree only.
/// </summary>
public static class LeftoverCandidateFinder
{
    private const string AppDataName = "AppData";
    private const string PackagesName = "Packages";
    private const string MicrosoftJoined = "microsoft";

    private static readonly string[] AppDataAreas = ["Roaming", "Local", "LocalLow"];

    /// <summary>Finds the candidates below <paramref name="profileRoot"/>.</summary>
    /// <param name="profileRoot">The profile root of the scan.</param>
    /// <param name="index">The evidence, used to recognise vendor folders.</param>
    /// <param name="ruleMatches">The rule matches; folders covered by protection or cache rules are skipped.</param>
    public static IReadOnlyList<LeftoverCandidate> Find(FolderNode profileRoot, EvidenceIndex index, RuleMatchMap ruleMatches)
    {
        ArgumentNullException.ThrowIfNull(profileRoot);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(ruleMatches);

        var candidates = new List<LeftoverCandidate>();

        foreach (FolderNode child in profileRoot.Children)
        {
            if (child.Name.Length > 1 && child.Name[0] == '.')
            {
                AddIfEligible(candidates, child, CandidateKind.DotFolder, ruleMatches);
            }
            else if (IsNamed(child, AppDataName))
            {
                FindInAppData(child, index, ruleMatches, candidates);
            }
        }

        return candidates;
    }

    /// <summary>
    /// True when the folder may be evaluated at all: not personal, not a link, not an unread cloud folder, readable, and not
    /// covered by a protected rule or a cache / temp / logs / crash-dump rule.
    /// </summary>
    public static bool IsEligible(FolderNode folder, RuleMatchMap ruleMatches)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(ruleMatches);

        if (folder.IsPersonal || folder.IsSkippedLink || folder.IsCloudFolderNotScanned || folder.IsAccessDenied || folder.HasError)
        {
            return false;
        }

        FolderRuleMatch? match = ruleMatches.Get(folder);
        return match is null
            || (!match.All.Any(item => item.Rule.Kind == RuleKind.Protected) && match.Primary.Rule.Kind != RuleKind.Cache);
    }

    private static void FindInAppData(FolderNode appData, EvidenceIndex index, RuleMatchMap ruleMatches, List<LeftoverCandidate> candidates)
    {
        foreach (FolderNode area in appData.Children.Where(child => AppDataAreas.Any(name => IsNamed(child, name))))
        {
            bool isLocal = IsNamed(area, "Local");
            foreach (FolderNode folder in area.Children)
            {
                if (isLocal && IsNamed(folder, PackagesName))
                {
                    foreach (FolderNode package in folder.Children)
                    {
                        AddIfEligible(candidates, package, CandidateKind.StorePackage, ruleMatches);
                    }

                    continue;
                }

                if (!IsEligible(folder, ruleMatches))
                {
                    continue;
                }

                NormalizedName name = NameNormalizer.Normalize(folder.Name);
                if (name.Joined == MicrosoftJoined)
                {
                    AddChildren(candidates, folder, CandidateKind.MicrosoftChild, ruleMatches);
                }
                else if (!GenericNames.IsGeneric(name) && index.Match(name) is { OnlyPublisher: true })
                {
                    AddChildren(candidates, folder, CandidateKind.VendorChild, ruleMatches);
                }
                else
                {
                    candidates.Add(new LeftoverCandidate(folder, CandidateKind.AppDataFolder));
                }
            }
        }
    }

    private static void AddChildren(List<LeftoverCandidate> candidates, FolderNode parent, CandidateKind kind, RuleMatchMap ruleMatches)
    {
        foreach (FolderNode child in parent.Children)
        {
            AddIfEligible(candidates, child, kind, ruleMatches);
        }
    }

    private static void AddIfEligible(List<LeftoverCandidate> candidates, FolderNode folder, CandidateKind kind, RuleMatchMap ruleMatches)
    {
        if (IsEligible(folder, ruleMatches))
        {
            candidates.Add(new LeftoverCandidate(folder, kind));
        }
    }

    private static bool IsNamed(FolderNode folder, string name) =>
        string.Equals(folder.Name, name, StringComparison.OrdinalIgnoreCase);
}

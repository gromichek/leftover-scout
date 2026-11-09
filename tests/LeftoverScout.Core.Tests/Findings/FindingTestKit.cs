using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Classification;
using LeftoverScout.Core.Tests.Rules;
using static LeftoverScout.Core.Tests.Classification.Evidences;

namespace LeftoverScout.Core.Tests.Findings;

/// <summary>Builds fictional scan results for the findings tests (no real profile is touched).</summary>
internal static class FindingTestKit
{
    public static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime Old = new(2023, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Runs the real rule matcher and classifier on <paramref name="root"/> and wraps the outcome in a result.</summary>
    public static ScanResult Result(FolderNode root, params string[] ruleJson)
    {
        RuleSet rules = ruleJson.Length == 0 ? RuleSet.Empty : RuleTestHelper.Load(ruleJson).RuleSet;
        RuleMatchMap matches = new RuleMatcher(rules).Match(root, CancellationToken.None);
        LeftoverMap leftovers = new LeftoverClassifier(new FixedTimeProvider(new DateTimeOffset(Now)))
            .Classify(root, Evidences.Snapshot(Registry("Unrelated Program")), matches, CancellationToken.None);
        return Wrap(root, matches, leftovers);
    }

    /// <summary>A result with hand-made verdicts, to prove that the builder re-checks safety itself.</summary>
    public static ScanResult Result(FolderNode root, string[] ruleJson, params (string Path, LeftoverVerdict Verdict)[] verdicts)
    {
        RuleSet rules = ruleJson.Length == 0 ? RuleSet.Empty : RuleTestHelper.Load(ruleJson).RuleSet;
        RuleMatchMap matches = new RuleMatcher(rules).Match(root, CancellationToken.None);
        var map = new Dictionary<FolderNode, LeftoverVerdict>();
        foreach ((string path, LeftoverVerdict verdict) in verdicts)
        {
            map[RuleTestHelper.Find(root, path)] = verdict;
        }

        return Wrap(root, matches, new LeftoverMap(map));
    }

    public static LeftoverVerdict Leftover(LeftoverConfidence confidence = LeftoverConfidence.Medium) =>
        LeftoverVerdict.Leftover(confidence, "Sample explanation.");

    private static ScanResult Wrap(FolderNode root, RuleMatchMap matches, LeftoverMap leftovers) =>
        new(ScanCompletion.Completed, root, [], [], 0, 0, 0, TimeSpan.Zero)
        {
            CompletedAt = DateTimeOffset.UnixEpoch,
            RuleMatches = matches,
            Leftovers = leftovers,
        };
}

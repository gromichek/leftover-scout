using System.Globalization;
using System.Text.RegularExpressions;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Classification;

/// <summary>
/// Decides, offline and explainably, whether a folder probably belongs to a program that is no longer installed
/// (decisions D-044 … D-047). Pure: no file system, no registry; the clock is injected.
/// </summary>
/// <remarks>
/// Safety bias: when in doubt the answer is "not a leftover". Verdicts:
/// <list type="bullet">
/// <item>InUse wins over everything: any evidence match, or an ownership rule whose hints match.</item>
/// <item>High: a Store package folder of a package that is not installed and unchanged for 6 months; an ownership rule
/// with <c>requiresInstallation: true</c> whose program is not found.</item>
/// <item>Medium: a Store package folder changed within 6 months; a candidate without evidence that is unchanged for
/// 6 months or has no files.</item>
/// <item>Low: a candidate without evidence that changed within 6 months.</item>
/// <item>Unknown: generic or unusable names, children of Microsoft, tools that need no installer, and everything when the
/// installed programs could not be read completely.</item>
/// </list>
/// A folder counts as "not changed" when its newest file is exactly 6 months old or older.
/// </remarks>
public sealed partial class LeftoverClassifier
{
    /// <summary>Number of months without changes after which a folder counts as not changed (decision D-046).</summary>
    public const int UnchangedMonths = 6;

    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the classifier.</summary>
    /// <param name="timeProvider">The clock used to decide what "not changed for 6 months" means.</param>
    public LeftoverClassifier(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    [GeneratedRegex(@"^\{?[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\}?$", RegexOptions.IgnoreCase)]
    private static partial Regex GuidName();

    /// <summary>
    /// Classifies the candidate folders of a scan. Returns an empty map when the evidence or the rule matches are missing,
    /// because without them nothing can be decided safely.
    /// </summary>
    /// <param name="root">The profile root of the scan.</param>
    /// <param name="evidence">The installed programs collected with the scan; null gives an empty map.</param>
    /// <param name="ruleMatches">The rule matches of the scan; null gives an empty map.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public LeftoverMap Classify(FolderNode root, EvidenceSnapshot? evidence, RuleMatchMap? ruleMatches, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (evidence is null || ruleMatches is null)
        {
            return LeftoverMap.Empty;
        }

        var index = new EvidenceIndex(evidence);
        bool evidenceComplete = evidence.Errors.Count == 0;
        DateTime cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddMonths(-UnchangedMonths);
        var verdicts = new Dictionary<FolderNode, LeftoverVerdict>();

        foreach (LeftoverCandidate candidate in LeftoverCandidateFinder.Find(root, index, ruleMatches))
        {
            cancellationToken.ThrowIfCancellationRequested();
            LeftoverVerdict verdict = Evaluate(candidate, index, ruleMatches.Get(candidate.Node), cutoff);
            if (verdict.Kind == LeftoverVerdictKind.Leftover && !evidenceComplete)
            {
                verdict = LeftoverVerdict.Unknown("The installed programs could not be read completely, so no decision was made.");
            }

            verdicts[candidate.Node] = verdict;
        }

        return new LeftoverMap(verdicts);
    }

    private static LeftoverVerdict Evaluate(LeftoverCandidate candidate, EvidenceIndex index, FolderRuleMatch? ruleMatch, DateTime cutoff)
    {
        FolderNode folder = candidate.Node;

        return candidate.Kind switch
        {
            CandidateKind.MicrosoftChild => LeftoverVerdict.Unknown("Folders of Microsoft and Windows components are not evaluated yet."),
            CandidateKind.StorePackage => EvaluatePackage(folder, index, cutoff),
            _ => EvaluateFolder(candidate, index, ruleMatch, cutoff),
        };
    }

    private static LeftoverVerdict EvaluatePackage(FolderNode folder, EvidenceIndex index, DateTime cutoff)
    {
        string family = folder.Name;
        if (index.FindStoreFamily(family) is { } installed)
        {
            return LeftoverVerdict.InUse(installed, $"Store package '{family}' is installed for this user ({installed}).");
        }

        string notInstalled = $"Store package '{family}' is not installed for this user.";
        if (IsUnchanged(folder, cutoff))
        {
            return LeftoverVerdict.Leftover(LeftoverConfidence.High, $"{notInstalled} Not changed since {Month(folder.NewestWriteTimeUtc!.Value)}.");
        }

        return LeftoverVerdict.Leftover(LeftoverConfidence.Medium, $"{notInstalled} {ActivityText(folder, "Its data")}");
    }

    private static LeftoverVerdict EvaluateFolder(LeftoverCandidate candidate, EvidenceIndex index, FolderRuleMatch? ruleMatch, DateTime cutoff)
    {
        FolderNode folder = candidate.Node;
        NormalizedName name = NameNormalizer.Normalize(folder.Name);

        if (name.IsEmpty || GuidName().IsMatch(folder.Name))
        {
            return LeftoverVerdict.Unknown($"The folder name '{folder.Name}' does not say which program it belongs to.");
        }

        if (GenericNames.IsGeneric(name))
        {
            return LeftoverVerdict.Unknown($"'{folder.Name}' is a generic folder name; it does not say which program it belongs to.");
        }

        if (index.Match(name) is { } match)
        {
            return LeftoverVerdict.InUse(match.ProgramName, $"Belongs to {match.ProgramName} (installed).");
        }

        ProgramHints? program = ruleMatch?.All.FirstOrDefault(item => item.Rule.Kind == RuleKind.Ownership && item.Rule.Program is not null)?.Rule.Program;
        if (program is not null)
        {
            return EvaluateOwnedFolder(program, index);
        }

        LeftoverVerdict verdict = EvaluateByTime(folder, cutoff);

        // Folders that start with a dot are often settings of command-line tools without an installer (.ssh, .docker,
        // .kube ...). Without a rule that knows the program, they are never rated above Low.
        if (candidate.Kind == CandidateKind.DotFolder && verdict.Confidence is { } confidence && confidence > LeftoverConfidence.Low)
        {
            return LeftoverVerdict.Leftover(
                LeftoverConfidence.Low,
                $"{verdict.Explanation} Folders that start with a dot often hold settings of tools without an installer, so they are rated Low at most.");
        }

        return verdict;
    }

    private static LeftoverVerdict EvaluateOwnedFolder(ProgramHints program, EvidenceIndex index)
    {
        if (HintsMatch(program, index))
        {
            return LeftoverVerdict.InUse(program.DisplayName, $"Belongs to {program.DisplayName} (installed).");
        }

        if (program.RequiresInstallation)
        {
            return LeftoverVerdict.Leftover(
                LeftoverConfidence.High,
                $"This folder belongs to {program.DisplayName}, which was not found among the installed programs.");
        }

        return LeftoverVerdict.Unknown(
            $"This folder belongs to {program.DisplayName}, a tool that often has no installer entry, so a missing entry proves nothing.");
    }

    private static bool HintsMatch(ProgramHints program, EvidenceIndex index) =>
        program.PackageFamilies.Any(hint => index.MatchesHint(hint))
        || new[] { program.DisplayName }.Concat(program.Names).Concat(program.UninstallKeys).Concat(program.ProgramFolders)
            .Any(hint => index.MatchesHint(hint));

    private static LeftoverVerdict EvaluateByTime(FolderNode folder, DateTime cutoff)
    {
        string notFound = $"No installed program named '{folder.Name}' was found.";

        if (folder.FileCount == 0 || folder.NewestWriteTimeUtc is null)
        {
            return LeftoverVerdict.Leftover(LeftoverConfidence.Medium, $"{notFound} The folder contains no files.");
        }

        if (IsUnchanged(folder, cutoff))
        {
            return LeftoverVerdict.Leftover(LeftoverConfidence.Medium, $"{notFound} Not changed since {Month(folder.NewestWriteTimeUtc.Value)}.");
        }

        return LeftoverVerdict.Leftover(LeftoverConfidence.Low, $"{notFound} {ActivityText(folder, "It")}");
    }

    // "Not changed" means: the newest file is exactly 6 months old or older. A folder without files is never "unchanged" here.
    private static bool IsUnchanged(FolderNode folder, DateTime cutoff) =>
        folder.NewestWriteTimeUtc is { } newest && newest <= cutoff;

    private static string ActivityText(FolderNode folder, string subject) =>
        folder.NewestWriteTimeUtc is { } newest
            ? $"{subject} changed in {Month(newest)}, so the program may still be used."
            : "The folder contains no files.";

    private static string Month(DateTime utc) => utc.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}

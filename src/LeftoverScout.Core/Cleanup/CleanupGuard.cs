using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// The one gate every deletion must pass (task T-009). For each folder the user selected it answers allowed or blocked,
/// from a fresh check of the path text, the rules, a hard-coded deny list and the real file system. It never trusts the
/// scan result and never changes anything: only attributes and the names of the direct entries are read.
/// </summary>
public sealed class CleanupGuard : ICleanupGuard
{
    private readonly RuleMatcher _matcher;
    private readonly IPathProbe _probe;
    private readonly IDirectoryReader _reader;
    private readonly IPersonalFolderProvider _personalFolders;
    private readonly IReadOnlyList<string> _appFolders;
    private readonly ILogger<CleanupGuard> _logger;

    /// <summary>Creates a guard.</summary>
    /// <param name="ruleSet">The rules; only the protected rules matter here.</param>
    /// <param name="probe">The live file system checks.</param>
    /// <param name="reader">Lists the direct entries of a folder (for the "changed since the scan" warning).</param>
    /// <param name="personalFolders">Supplies the known personal folder paths.</param>
    /// <param name="appFolders">The program folder and the data folder of LeftoverScout.</param>
    /// <param name="logger">The logger; only counts are logged, never paths.</param>
    public CleanupGuard(
        RuleSet ruleSet,
        IPathProbe probe,
        IDirectoryReader reader,
        IPersonalFolderProvider personalFolders,
        IEnumerable<string> appFolders,
        ILogger<CleanupGuard> logger)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(personalFolders);
        ArgumentNullException.ThrowIfNull(appFolders);
        ArgumentNullException.ThrowIfNull(logger);

        _matcher = new RuleMatcher(ruleSet);
        _probe = probe;
        _reader = reader;
        _personalFolders = personalFolders;
        _appFolders = [.. appFolders];
        _logger = logger;
    }

    /// <summary>
    /// Checks a request: first merges duplicates and nested items, then checks every remaining item. The cheap path
    /// checks run first; the live checks run only for items that passed them.
    /// </summary>
    public CleanupGuardReport Check(CleanupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        RequestReduction reduction = RequestReducer.Reduce(request.Items);
        var checker = new CleanupPathChecker(request.Target, _matcher, _personalFolders.GetPersonalFolderPaths(), _appFolders);

        var results = new List<GuardItemResult>(reduction.Items.Count);
        foreach (CleanupItem item in reduction.Items)
        {
            results.Add(new GuardItemResult(item, CheckItem(request.Target, checker, item)));
        }

        LogCounts(reduction, results);
        return new CleanupGuardReport(reduction, results);
    }

    private GuardResult CheckItem(ScanTarget target, CleanupPathChecker checker, CleanupItem item)
    {
        PathParseResult parseResult = ParsedPath.TryParse(item.FullPath, out ParsedPath? parsed);
        if (parseResult != PathParseResult.Ok)
        {
            return Blocked(checker.Check(item.FullPath));
        }

        List<BlockReason> pure = checker.Check(parsed!);
        if (pure.Count > 0)
        {
            return Blocked(pure);
        }

        List<BlockReason> live = CheckLive(target, checker, parsed!, out ParsedPath? longForm);
        if (live.Count > 0)
        {
            return Blocked(live);
        }

        return new GuardResult([], CollectWarnings(item, longForm!));
    }

    // Live checks: existence, long form, links and cloud folders on the way. Only attribute queries.
    private List<BlockReason> CheckLive(ScanTarget target, CleanupPathChecker checker, ParsedPath given, out ParsedPath? longForm)
    {
        longForm = null;

        PathProbe itemProbe = _probe.Probe(given.Text);
        if (itemProbe.Status == PathProbeStatus.NotFound)
        {
            return [BlockReason.Of(BlockReasonCode.NotFound)];
        }

        if (itemProbe.Status != PathProbeStatus.Found)
        {
            return [BlockReason.Of(BlockReasonCode.CouldNotBeChecked)];
        }

        // The long form resolves 8.3 short names (APPDAT~1) that could hide a protected folder.
        string? longText = _probe.GetLongPath(given.Text);
        ParsedPath? resolved = ParsedPath.ParseOrNull(longText);
        if (resolved is null)
        {
            return [BlockReason.Of(BlockReasonCode.CouldNotBeChecked)];
        }

        if (!resolved.IsSameAs(given))
        {
            List<BlockReason> again = checker.Check(resolved);
            if (again.Count > 0)
            {
                return again;
            }
        }

        ParsedPath profile = ParsedPath.ParseOrNull(target.ProfileRootPath)!;
        if (!resolved.IsStrictlyInside(profile))
        {
            return [BlockReason.Of(BlockReasonCode.NotInsideProfile)];
        }

        // Every folder from the one below the profile root down to the item itself.
        for (int length = profile.Segments.Length + 1; length <= resolved.Segments.Length; length++)
        {
            bool isItem = length == resolved.Segments.Length;
            ParsedPath current = resolved.Prefix(length);
            PathProbe probe = isItem && resolved.IsSameAs(given) ? itemProbe : _probe.Probe(current.Text);

            BlockReason? reason = CheckOne(probe, isItem);
            if (reason is not null)
            {
                return [reason];
            }
        }

        longForm = resolved;
        return [];
    }

    private static BlockReason? CheckOne(PathProbe probe, bool isItem)
    {
        if (probe.Status == PathProbeStatus.NotFound)
        {
            return BlockReason.Of(BlockReasonCode.NotFound);
        }

        if (probe.Status != PathProbeStatus.Found)
        {
            return BlockReason.Of(BlockReasonCode.CouldNotBeChecked);
        }

        bool isReparsePoint = (probe.Attributes & EntryClassifier.AttributeReparsePoint) != 0;
        if (isReparsePoint && EntryClassifier.IsNameSurrogate(probe.ReparseTag))
        {
            return BlockReason.Of(isItem ? BlockReasonCode.ReparsePoint : BlockReasonCode.ReparsePointInPath);
        }

        if ((probe.Attributes & EntryClassifier.AttributeDirectory) == 0)
        {
            return BlockReason.Of(BlockReasonCode.NotADirectory);
        }

        return (probe.Attributes & EntryClassifier.AttributeRecallOnOpen) != 0
            ? BlockReason.Of(BlockReasonCode.CloudNotPopulated)
            : null;
    }

    private List<GuardWarning> CollectWarnings(CleanupItem item, ParsedPath longForm)
    {
        var warnings = new List<GuardWarning>();

        if (HasChangedSinceScan(item, longForm))
        {
            warnings.Add(GuardWarning.Of(GuardWarningCode.ChangedSinceScan));
        }

        switch (item.Confidence)
        {
            case LeftoverConfidence.Low:
                warnings.Add(GuardWarning.Of(GuardWarningCode.LowConfidence));
                break;
            case LeftoverConfidence.Medium:
                warnings.Add(GuardWarning.Of(GuardWarningCode.MediumConfidence));
                break;
        }

        return warnings;
    }

    // Compares the newest write time of the direct entries with what the scan saw. Listing only enumerates.
    private bool HasChangedSinceScan(CleanupItem item, ParsedPath folder)
    {
        DirectoryListing listing = _reader.ReadDirectory(folder.Text);
        if (listing.Entries.Count == 0)
        {
            return false;
        }

        if (item.NewestWriteTimeUtc is not { } scanned)
        {
            // The scan saw no files: any file now is new.
            return listing.Entries.Any(entry => !entry.IsDirectory);
        }

        return listing.Entries.Max(entry => entry.LastWriteTimeUtc) > scanned;
    }

    private static GuardResult Blocked(IReadOnlyList<BlockReason> reasons) => new(reasons, []);

    private void LogCounts(RequestReduction reduction, List<GuardItemResult> results)
    {
        int allowed = results.Count(result => result.Result.IsAllowed);
        var perReason = results
            .SelectMany(result => result.Result.BlockReasons)
            .GroupBy(reason => reason.Code)
            .Select(group => $"{group.Key}={group.Count()}");

        _logger.LogInformation(
            "Cleanup guard checked {Items} items: {Allowed} allowed, {Blocked} blocked, {Merged} merged. Block reasons: {Reasons}.",
            results.Count,
            allowed,
            results.Count - allowed,
            reduction.Merged.Count,
            string.Join(", ", perReason));
    }
}

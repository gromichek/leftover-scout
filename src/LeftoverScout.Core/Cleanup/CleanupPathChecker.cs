using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// The pure path checks of the cleanup guard (task T-009, section 3). It looks only at the text of the path, the
/// profile root, the known personal folders, the folders of the app and the rules; it never touches the file system.
/// It is a second, independent line of protection: it does not use the result of the Findings builder.
/// </summary>
public sealed class CleanupPathChecker
{
    private readonly ParsedPath? _profile;
    private readonly RuleMatcher _matcher;
    private readonly List<ParsedPath> _personalPaths = [];
    private readonly List<ParsedPath> _appFolders = [];

    /// <summary>Creates a checker.</summary>
    /// <param name="target">The profile; only paths strictly inside its root are allowed.</param>
    /// <param name="matcher">The rule matcher (protected rules).</param>
    /// <param name="personalFolderPaths">The known personal folder paths (Documents, OneDrive roots, ...).</param>
    /// <param name="appFolderPaths">The program folder and the data folder of LeftoverScout.</param>
    public CleanupPathChecker(
        ScanTarget target,
        RuleMatcher matcher,
        IEnumerable<string> personalFolderPaths,
        IEnumerable<string> appFolderPaths)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(personalFolderPaths);
        ArgumentNullException.ThrowIfNull(appFolderPaths);

        _profile = ParsedPath.ParseOrNull(target.ProfileRootPath);
        _matcher = matcher;
        _personalPaths.AddRange(personalFolderPaths.Select(ParsedPath.ParseOrNull).OfType<ParsedPath>());
        _appFolders.AddRange(appFolderPaths.Select(ParsedPath.ParseOrNull).OfType<ParsedPath>());
    }

    /// <summary>Returns every reason why <paramref name="path"/> must not be deleted; empty when the text of the path is acceptable.</summary>
    public IReadOnlyList<BlockReason> Check(string? path)
    {
        PathParseResult result = ParsedPath.TryParse(path, out ParsedPath? parsed);
        return result switch
        {
            PathParseResult.Device => [BlockReason.Of(BlockReasonCode.DevicePath)],
            PathParseResult.Invalid => [BlockReason.Of(BlockReasonCode.InvalidPath)],
            _ => Check(parsed!),
        };
    }

    /// <summary>Checks an already parsed path.</summary>
    internal List<BlockReason> Check(ParsedPath path)
    {
        var reasons = new List<BlockReason>();

        if (_appFolders.Any(app => path.IsAtOrInside(app) || app.IsAtOrInside(path)))
        {
            reasons.Add(BlockReason.Of(BlockReasonCode.AppFolder));
        }

        if (_profile is null || !path.IsStrictlyInside(_profile))
        {
            reasons.Add(_profile is not null && path.IsSameAs(_profile)
                ? BlockReason.Of(BlockReasonCode.FixedRoot)
                : BlockReason.Of(BlockReasonCode.NotInsideProfile));
            return reasons;
        }

        string[] relative = path.Segments[_profile.Segments.Length..];

        if (ProtectedDenyList.IsFixedRoot(relative))
        {
            reasons.Add(BlockReason.Of(BlockReasonCode.FixedRoot));
        }

        if (ProtectedDenyList.Blocks(relative))
        {
            reasons.Add(BlockReason.Of(BlockReasonCode.DenyList));
        }

        if (IsPersonal(path, relative))
        {
            reasons.Add(BlockReason.Of(BlockReasonCode.PersonalFolder));
        }

        FolderRuleMatch? match = _matcher.MatchPath(relative);
        RuleMatch? protectedMatch = match?.All.FirstOrDefault(m => m.Rule.Kind == RuleKind.Protected);
        if (protectedMatch is not null)
        {
            reasons.Add(BlockReason.Of(BlockReasonCode.ProtectedByRule, protectedMatch.Rule.Id));
        }
        else if (_matcher.ContainsProtectedPath(relative))
        {
            reasons.Add(BlockReason.Of(BlockReasonCode.ProtectedByRule, "contains a protected folder"));
        }

        return reasons;
    }

    // Personal when the folder or any ancestor is personal, or when the folder contains a known personal folder.
    private bool IsPersonal(ParsedPath path, string[] relative)
    {
        for (int length = 1; length <= relative.Length; length++)
        {
            string name = relative[length - 1];
            if (length == 1
                && (name.StartsWith(PersonalFolderMatcher.OneDrivePrefix, StringComparison.OrdinalIgnoreCase)
                    || PersonalFolderMatcher.PersonalNames.Contains(name, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }

            ParsedPath prefix = path.Prefix(_profile!.Segments.Length + length);
            if (_personalPaths.Any(personal => personal.IsSameAs(prefix)))
            {
                return true;
            }
        }

        return _personalPaths.Any(personal => personal.IsStrictlyInside(path));
    }
}

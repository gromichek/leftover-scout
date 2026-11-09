namespace LeftoverScout.Core.Rules;

/// <summary>
/// All rules that apply to one folder, best match first: protected rules win, then direct matches before
/// inherited ones, then the more specific pattern (decision D-041).
/// </summary>
public sealed class FolderRuleMatch
{
    internal FolderRuleMatch(IEnumerable<RuleMatch> matches)
    {
        All = matches
            .OrderBy(match => match.Rule.Kind == RuleKind.Protected ? 0 : 1)
            .ThenBy(match => match.IsInherited ? 1 : 0)
            .ThenByDescending(match => match.Specificity)
            .ThenBy(match => match.Rule.Id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Every matching rule, primary match first. Never empty.</summary>
    public IReadOnlyList<RuleMatch> All { get; }

    /// <summary>The best match.</summary>
    public RuleMatch Primary => All[0];

    /// <summary>True when the folder is protected: it must never be offered for deletion.</summary>
    public bool IsProtected => Primary.Rule.Kind == RuleKind.Protected;
}

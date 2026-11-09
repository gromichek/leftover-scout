namespace LeftoverScout.Core.Rules;

/// <summary>A rule that applies to a folder.</summary>
/// <param name="Rule">The rule.</param>
/// <param name="Pattern">The pattern that matched; null for the built-in personal-folder protection.</param>
/// <param name="IsInherited">True when the rule matched a parent folder and has subtree scope.</param>
public sealed record RuleMatch(Rule Rule, PathPattern? Pattern, bool IsInherited)
{
    /// <summary>How specific the match is (number of literal segments); the built-in personal protection is the most specific.</summary>
    public int Specificity => Pattern?.LiteralCount ?? int.MaxValue;

    /// <summary>True for the built-in protection of personal folders.</summary>
    public bool IsPersonalFolder => ReferenceEquals(Rule, RuleMatcher.PersonalFolderRule);
}

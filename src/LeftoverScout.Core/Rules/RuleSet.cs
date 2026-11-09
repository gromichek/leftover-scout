namespace LeftoverScout.Core.Rules;

/// <summary>
/// The validated rules, in file order.
/// </summary>
public sealed class RuleSet
{
    /// <summary>A rule set without rules.</summary>
    public static RuleSet Empty { get; } = new([]);

    /// <summary>Creates a rule set.</summary>
    public RuleSet(IReadOnlyList<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules;
    }

    /// <summary>All valid rules.</summary>
    public IReadOnlyList<Rule> Rules { get; }
}

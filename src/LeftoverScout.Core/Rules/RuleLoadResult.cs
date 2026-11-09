namespace LeftoverScout.Core.Rules;

/// <summary>The outcome of loading rule files: the valid rules and the errors of the skipped ones.</summary>
/// <param name="RuleSet">The valid rules.</param>
/// <param name="Errors">One entry per problem.</param>
public sealed record RuleLoadResult(RuleSet RuleSet, IReadOnlyList<RuleValidationError> Errors);

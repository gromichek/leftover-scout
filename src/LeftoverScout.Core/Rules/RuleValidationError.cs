namespace LeftoverScout.Core.Rules;

/// <summary>A problem found while loading a rule file. The rule (or the whole file) is skipped.</summary>
/// <param name="File">The file name.</param>
/// <param name="RuleId">The rule id; null when the problem concerns the whole file or the id is unknown.</param>
/// <param name="Message">A short English description.</param>
public sealed record RuleValidationError(string File, string? RuleId, string Message);

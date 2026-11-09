using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Rules;

/// <summary>
/// Loads the embedded rules for the running app. Invalid rules are skipped and logged by id; the app never crashes
/// because of a rule file.
/// </summary>
public static class EmbeddedRuleLoader
{
    /// <summary>Loads and validates the embedded rules.</summary>
    public static RuleSet Load(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        RuleLoadResult result;
        try
        {
            result = RuleLoader.Load(EmbeddedRuleFiles.Read());
        }
        catch (Exception exception)
        {
            logger.LogError("The rules could not be loaded ({ExceptionType}); continuing without rules.", exception.GetType().Name);
            return RuleSet.Empty;
        }

        foreach (RuleValidationError error in result.Errors)
        {
            logger.LogWarning(
                "Rule {RuleId} in {File} was skipped: {Message}",
                error.RuleId ?? "(unknown)",
                error.File.Length == 0 ? "(rules)" : error.File,
                error.Message);
        }

        logger.LogInformation("Loaded {RuleCount} rules.", result.RuleSet.Rules.Count);
        return result.RuleSet;
    }
}

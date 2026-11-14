namespace LeftoverScout.Core.Cleanup;

/// <summary>One row of the review table of the Cleanup page.</summary>
/// <param name="Item">The item.</param>
/// <param name="RelativePath">Path below the profile folder.</param>
/// <param name="KindText">Leftover, Cache, Temp, Logs or Crash dumps.</param>
/// <param name="ConfidenceText">High, Medium or Low; empty for cache kinds.</param>
/// <param name="IsReady">True when the guard allowed the item; only ready items are cleaned.</param>
/// <param name="StatusText"><c>Ready</c>, <c>Ready · &lt;warnings&gt;</c> or <c>Blocked: &lt;reason&gt;</c>.</param>
public sealed record CleanupReviewRow(
    CleanupItem Item,
    string RelativePath,
    string KindText,
    string ConfidenceText,
    bool IsReady,
    string StatusText);

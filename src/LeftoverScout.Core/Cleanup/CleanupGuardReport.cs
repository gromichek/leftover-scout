namespace LeftoverScout.Core.Cleanup;

/// <summary>The result of checking a whole request.</summary>
/// <param name="Reduction">Which items were merged before checking.</param>
/// <param name="Items">One result for each remaining item, in order.</param>
public sealed record CleanupGuardReport(RequestReduction Reduction, IReadOnlyList<GuardItemResult> Items);

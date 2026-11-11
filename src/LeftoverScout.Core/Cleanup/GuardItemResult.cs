namespace LeftoverScout.Core.Cleanup;

/// <summary>The guard result for one item of a request.</summary>
/// <param name="Item">The item.</param>
/// <param name="Result">Allowed, or blocked with reasons.</param>
public sealed record GuardItemResult(CleanupItem Item, GuardResult Result);

namespace LeftoverScout.Core.Cleanup;

/// <summary>The result of reducing a request: the items that remain and the items that were merged.</summary>
/// <param name="Items">The remaining items, in the original order.</param>
/// <param name="Merged">The items that were dropped, with the item that covers each one.</param>
public sealed record RequestReduction(IReadOnlyList<CleanupItem> Items, IReadOnlyList<MergedItem> Merged);

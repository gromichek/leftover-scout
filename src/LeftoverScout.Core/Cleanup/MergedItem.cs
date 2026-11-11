namespace LeftoverScout.Core.Cleanup;

/// <summary>An item that was removed from a request because another item covers it.</summary>
/// <param name="Item">The dropped item.</param>
/// <param name="CoveredBy">The full path of the item that covers it.</param>
/// <param name="Reason">Why it was dropped.</param>
public sealed record MergedItem(CleanupItem Item, string CoveredBy, MergeReason Reason);

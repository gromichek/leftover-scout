namespace LeftoverScout.Core.Cleanup;

/// <summary>A running program that seems to belong to an item (decision D-064). A warning, never a block.</summary>
/// <param name="Item">The item.</param>
/// <param name="ProcessNames">Names of the matching running processes, distinct and sorted.</param>
public sealed record RunningProgramWarning(CleanupItem Item, IReadOnlyList<string> ProcessNames);

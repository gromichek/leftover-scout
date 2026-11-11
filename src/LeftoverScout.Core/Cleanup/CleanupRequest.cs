using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// The items the user selected for cleanup in one profile.
/// </summary>
/// <param name="Target">The profile the items belong to.</param>
/// <param name="Items">The selected items, before duplicates and nested items are merged.</param>
public sealed record CleanupRequest(ScanTarget Target, IReadOnlyList<CleanupItem> Items);

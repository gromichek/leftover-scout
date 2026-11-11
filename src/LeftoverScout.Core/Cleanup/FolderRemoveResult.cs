namespace LeftoverScout.Core.Cleanup;

/// <summary>The result of removing one folder, including the locked-file fallback.</summary>
/// <param name="Status">What happened.</param>
/// <param name="ItemsLeft">Entries that remain (in use, or links that are never followed).</param>
/// <param name="Reason">Short English reason; no paths.</param>
/// <param name="DeletedPermanentlyUnexpectedly">Layer 3 flag from any entry.</param>
public sealed record FolderRemoveResult(
    FolderRemoveStatus Status,
    int ItemsLeft = 0,
    string? Reason = null,
    bool DeletedPermanentlyUnexpectedly = false);

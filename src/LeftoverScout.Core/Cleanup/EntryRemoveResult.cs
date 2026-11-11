namespace LeftoverScout.Core.Cleanup;

/// <summary>The result of removing one file or folder.</summary>
/// <param name="Status">What happened.</param>
/// <param name="Reason">A short English reason for failures; no paths.</param>
/// <param name="DeletedPermanentlyUnexpectedly">Layer 3: Recycle Bin mode, but Windows deleted it permanently.</param>
public sealed record EntryRemoveResult(EntryRemoveStatus Status, string? Reason = null, bool DeletedPermanentlyUnexpectedly = false);

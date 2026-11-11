namespace LeftoverScout.Core.Cleanup;

/// <summary>The honest result for one item.</summary>
/// <param name="Item">The item.</param>
/// <param name="Outcome">What happened.</param>
/// <param name="BytesRemoved">Bytes removed (estimated from the scan size and what remains); 0 when nothing was removed.</param>
/// <param name="ItemsLeft">For <see cref="CleanupOutcome.PartlyRemoved"/>: how many entries remain (in use, or links that are never followed).</param>
/// <param name="Reason">A short English reason for every outcome other than a complete removal; null otherwise.</param>
/// <param name="BlockReasons">For <see cref="CleanupOutcome.Blocked"/>: the guard reasons.</param>
/// <param name="DeletedPermanentlyUnexpectedly">
/// True when Recycle Bin mode was asked but Windows reported that the item was deleted permanently (D-062 layer 3).
/// </param>
public sealed record CleanupItemResult(
    CleanupItem Item,
    CleanupOutcome Outcome,
    long BytesRemoved = 0,
    int ItemsLeft = 0,
    string? Reason = null,
    IReadOnlyList<BlockReason>? BlockReasons = null,
    bool DeletedPermanentlyUnexpectedly = false);

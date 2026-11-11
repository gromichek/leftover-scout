namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Pure decisions of the layers 2 and 3 against silent permanent deletion (D-062). The Windows implementation of
/// <c>IFileOperationProgressSink</c> only forwards the values to these functions.
/// </summary>
public static class ShellOperationRules
{
    /// <summary>
    /// <c>TSF_DELETE_RECYCLE_IF_POSSIBLE</c> of <c>TRANSFER_SOURCE_FLAGS</c> (shobjidl_core.h; Microsoft Learn,
    /// "TRANSFER_SOURCE_FLAGS"). Windows sets it in <c>PreDeleteItem</c> when the item goes to the Recycle Bin.
    /// </summary>
    public const uint TransferDeleteRecycleIfPossible = 0x00000080;

    /// <summary>Layer 2: true when the delete of an item must be vetoed (Recycle Bin mode, flag missing).</summary>
    public static bool ShouldVetoDelete(CleanupMode mode, uint transferFlags) =>
        mode == CleanupMode.RecycleBin && (transferFlags & TransferDeleteRecycleIfPossible) == 0;

    /// <summary>
    /// Layer 3: true when an item that Windows reports as deleted was deleted permanently although the Recycle Bin was
    /// asked for (no new shell item in the bin).
    /// </summary>
    public static bool IsUnexpectedPermanentDelete(CleanupMode mode, bool deleteSucceeded, bool hasNewItemInBin) =>
        mode == CleanupMode.RecycleBin && deleteSucceeded && !hasNewItemInBin;
}

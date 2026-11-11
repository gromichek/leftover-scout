using System.Runtime.InteropServices.Marshalling;
using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Windows.Cleanup;

/// <summary>
/// Progress sink of one <c>IFileOperation</c> delete. It implements layer 2 (veto in <c>PreDeleteItem</c> when the
/// Recycle Bin flag is missing) and layer 3 (check of <c>psiNewlyCreated</c> in <c>PostDeleteItem</c>) of decision
/// D-062. The decisions themselves are in <see cref="ShellOperationRules"/>. It is used on one thread only.
/// </summary>
[GeneratedComClass]
internal sealed partial class FileOperationSink : IFileOperationProgressSink
{
    /// <summary>E_ABORT: tells the shell not to do this operation.</summary>
    private const int Abort = unchecked((int)0x80004004);

    private readonly CleanupMode _mode;

    public FileOperationSink(CleanupMode mode)
    {
        _mode = mode;
    }

    /// <summary>True when a delete was vetoed because it would not have gone to the Recycle Bin.</summary>
    public bool Vetoed { get; private set; }

    /// <summary>Number of <c>PostDeleteItem</c> calls.</summary>
    public int PostDeleteCount { get; private set; }

    /// <summary>The first failure HRESULT reported by <c>PostDeleteItem</c>; 0 when none failed.</summary>
    public int FirstFailure { get; private set; }

    /// <summary>True when an item was deleted permanently although the Recycle Bin was asked for.</summary>
    public bool UnexpectedPermanentDelete { get; private set; }

    public int PreDeleteItem(uint flags, nint item)
    {
        if (ShellOperationRules.ShouldVetoDelete(_mode, flags))
        {
            Vetoed = true;
            return Abort;
        }

        return 0;
    }

    public int PostDeleteItem(uint flags, nint item, int result, nint newlyCreated)
    {
        PostDeleteCount++;
        bool succeeded = result >= 0;
        if (!succeeded && FirstFailure == 0)
        {
            FirstFailure = result;
        }

        if (ShellOperationRules.IsUnexpectedPermanentDelete(_mode, succeeded, newlyCreated != 0))
        {
            UnexpectedPermanentDelete = true;
        }

        return 0;
    }

    public int StartOperations() => 0;

    public int FinishOperations(int result) => 0;

    public int PreRenameItem(uint flags, nint item, nint newName) => 0;

    public int PostRenameItem(uint flags, nint item, nint newName, int result, nint newlyCreated) => 0;

    public int PreMoveItem(uint flags, nint item, nint destinationFolder, nint newName) => 0;

    public int PostMoveItem(uint flags, nint item, nint destinationFolder, nint newName, int result, nint newlyCreated) => 0;

    public int PreCopyItem(uint flags, nint item, nint destinationFolder, nint newName) => 0;

    public int PostCopyItem(uint flags, nint item, nint destinationFolder, nint newName, int result, nint newlyCreated) => 0;

    public int PreNewItem(uint flags, nint destinationFolder, nint newName) => 0;

    public int PostNewItem(uint flags, nint destinationFolder, nint newName, nint templateName, uint attributes, int result, nint newItem) => 0;

    public int UpdateProgress(uint workTotal, uint workSoFar) => 0;

    public int ResetTimer() => 0;

    public int PauseTimer() => 0;

    public int ResumeTimer() => 0;
}

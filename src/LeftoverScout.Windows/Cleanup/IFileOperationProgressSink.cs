using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace LeftoverScout.Windows.Cleanup;

/// <summary>
/// COM interface <c>IFileOperationProgressSink</c> (shobjidl_core.h, IID 04b0f1a7-9490-44bc-96e1-4296a31252e2). The
/// methods are in vtable order, which must not change. Shell item and string parameters are raw pointers: the sink
/// only checks them for null and never releases them (they belong to the caller).
/// </summary>
[GeneratedComInterface]
[Guid("04b0f1a7-9490-44bc-96e1-4296a31252e2")]
internal partial interface IFileOperationProgressSink
{
    [PreserveSig]
    int StartOperations();

    [PreserveSig]
    int FinishOperations(int result);

    [PreserveSig]
    int PreRenameItem(uint flags, nint item, nint newName);

    [PreserveSig]
    int PostRenameItem(uint flags, nint item, nint newName, int result, nint newlyCreated);

    [PreserveSig]
    int PreMoveItem(uint flags, nint item, nint destinationFolder, nint newName);

    [PreserveSig]
    int PostMoveItem(uint flags, nint item, nint destinationFolder, nint newName, int result, nint newlyCreated);

    [PreserveSig]
    int PreCopyItem(uint flags, nint item, nint destinationFolder, nint newName);

    [PreserveSig]
    int PostCopyItem(uint flags, nint item, nint destinationFolder, nint newName, int result, nint newlyCreated);

    [PreserveSig]
    int PreDeleteItem(uint flags, nint item);

    [PreserveSig]
    int PostDeleteItem(uint flags, nint item, int result, nint newlyCreated);

    [PreserveSig]
    int PreNewItem(uint flags, nint destinationFolder, nint newName);

    [PreserveSig]
    int PostNewItem(uint flags, nint destinationFolder, nint newName, nint templateName, uint attributes, int result, nint newItem);

    [PreserveSig]
    int UpdateProgress(uint workTotal, uint workSoFar);

    [PreserveSig]
    int ResetTimer();

    [PreserveSig]
    int PauseTimer();

    [PreserveSig]
    int ResumeTimer();
}

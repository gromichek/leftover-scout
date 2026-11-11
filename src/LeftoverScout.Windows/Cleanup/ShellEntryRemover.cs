using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Windows.Interop;

namespace LeftoverScout.Windows.Cleanup;

/// <summary>
/// Removes one file or folder with the Windows shell operation <c>IFileOperation</c> (the only code in the app that
/// deletes user data). Every call runs on its own short-lived STA thread, never on the UI thread, and releases every COM
/// object before the thread ends.
/// </summary>
/// <remarks>
/// Recycle Bin mode sets <c>FOFX_RECYCLEONDELETE | FOF_ALLOWUNDO | FOF_WANTNUKEWARNING</c> and installs a progress sink that
/// vetoes any delete that would not go to the bin and checks that a recycled item has a new item in the bin
/// (D-062 layers 2 and 3). Permanent mode sets neither recycle flag. <c>FOFX_NOSKIPJUNCTIONS</c> is never set, so the shell
/// does not walk through junctions. COM calls use the documented vtable order of <c>IFileOperation</c> (shobjidl_core.h,
/// Microsoft Learn "IFileOperation interface"); the IUnknown methods take slots 0 to 2.
/// </remarks>
public sealed class ShellEntryRemover : IEntryRemover
{
    private const uint FofSilent = 0x0004;
    private const uint FofNoConfirmation = 0x0010;
    private const uint FofAllowUndo = 0x0040;
    private const uint FofNoConfirmMkDir = 0x0200;
    private const uint FofNoErrorUi = 0x0400;
    private const uint FofWantNukeWarning = 0x4000;
    private const uint FofxRecycleOnDelete = 0x00080000;
    private const uint ClsctxInprocServer = 1;

    private const int HResultFileNotFound = unchecked((int)0x80070002);
    private const int HResultPathNotFound = unchecked((int)0x80070003);

    // vtable slots of IFileOperation (after IUnknown: QueryInterface 0, AddRef 1, Release 2).
    private const int SlotAdvise = 3;
    private const int SlotUnadvise = 4;
    private const int SlotSetOperationFlags = 5;
    private const int SlotDeleteItem = 18;
    private const int SlotPerformOperations = 21;

    private static readonly Guid ClsidFileOperation = new("3ad05575-8857-4850-9277-11b85bdb8e09");
    private static readonly Guid IidFileOperation = new("947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8");
    private static readonly Guid IidShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
    private static readonly Guid IidSink = new("04b0f1a7-9490-44bc-96e1-4296a31252e2");

    private readonly IPathProbe _probe;

    /// <summary>Creates the remover.</summary>
    /// <param name="probe">Used to confirm that a folder is really gone after the shell reports success.</param>
    public ShellEntryRemover(IPathProbe probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        _probe = probe;
    }

    /// <inheritdoc />
    public EntryRemoveResult Remove(string path, CleanupMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        EntryRemoveResult? result = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = RemoveOnStaThread(path, mode);
            }
            catch (Exception exception) when (exception is COMException or InvalidOperationException or ArgumentException or IOException)
            {
                result = new EntryRemoveResult(EntryRemoveStatus.Failed, $"The Windows file operation failed ({exception.GetType().Name}).");
            }
        })
        {
            IsBackground = true,
            Name = "LeftoverScout cleanup (STA)",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result ?? new EntryRemoveResult(EntryRemoveStatus.Failed, "The Windows file operation did not finish.");
    }

    private unsafe EntryRemoveResult RemoveOnStaThread(string path, CleanupMode mode)
    {
        nint operation = 0;
        nint item = 0;
        nint sinkUnknown = 0;
        nint sinkPointer = 0;
        uint cookie = 0;
        bool advised = false;

        try
        {
            int hr = NativeMethods.CoCreateInstance(in ClsidFileOperation, 0, ClsctxInprocServer, in IidFileOperation, out operation);
            if (hr < 0)
            {
                return Failed($"The Windows file operation could not be started (0x{hr:X8}).");
            }

            hr = NativeMethods.SHCreateItemFromParsingName(path, 0, in IidShellItem, out item);
            if (hr < 0)
            {
                return hr is HResultFileNotFound or HResultPathNotFound
                    ? new EntryRemoveResult(EntryRemoveStatus.NotFound, "The folder does not exist any more.")
                    : Failed($"The folder could not be opened by Windows (0x{hr:X8}).");
            }

            var sink = new FileOperationSink(mode);
            sinkUnknown = new StrategyBasedComWrappers().GetOrCreateComInterfaceForObject(sink, CreateComInterfaceFlags.None);
            Guid sinkId = IidSink;
            hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)VTable(sinkUnknown)[0])(sinkUnknown, &sinkId, &sinkPointer);

            if (hr < 0 || sinkPointer == 0)
            {
                return Failed($"The safety check of the Windows file operation could not be installed (0x{hr:X8}).");
            }

            uint flags = FofSilent | FofNoConfirmation | FofNoErrorUi | FofNoConfirmMkDir;
            if (mode == CleanupMode.RecycleBin)
            {
                flags |= FofxRecycleOnDelete | FofAllowUndo | FofWantNukeWarning;
            }

            hr = ((delegate* unmanaged[Stdcall]<nint, uint, int>)VTable(operation)[SlotSetOperationFlags])(operation, flags);
            if (hr < 0)
            {
                return Failed($"The Windows file operation could not be configured (0x{hr:X8}).");
            }

            uint adviseCookie = 0;
            hr = ((delegate* unmanaged[Stdcall]<nint, nint, uint*, int>)VTable(operation)[SlotAdvise])(operation, sinkPointer, &adviseCookie);
            if (hr < 0)
            {
                return Failed($"The safety check of the Windows file operation could not be attached (0x{hr:X8}).");
            }

            cookie = adviseCookie;
            advised = true;

            hr = ((delegate* unmanaged[Stdcall]<nint, nint, nint, int>)VTable(operation)[SlotDeleteItem])(operation, item, 0);
            if (hr < 0)
            {
                return Failed($"The folder could not be queued for removal (0x{hr:X8}).");
            }

            int performResult = ((delegate* unmanaged[Stdcall]<nint, int>)VTable(operation)[SlotPerformOperations])(operation);
            return Interpret(sink, performResult, path, mode);
        }
        finally
        {
            if (advised)
            {
                ((delegate* unmanaged[Stdcall]<nint, uint, int>)VTable(operation)[SlotUnadvise])(operation, cookie);
            }

            Release(sinkPointer);
            Release(sinkUnknown);
            Release(item);
            Release(operation);
        }
    }

    private EntryRemoveResult Interpret(FileOperationSink sink, int performResult, string path, CleanupMode mode)
    {
        if (sink.Vetoed)
        {
            return new EntryRemoveResult(
                EntryRemoveStatus.NotRecycled,
                "Windows could not move it to the Recycle Bin (too large, or the bin is turned off); nothing was deleted.");
        }

        int failure = sink.FirstFailure != 0 ? sink.FirstFailure : (sink.PostDeleteCount == 0 ? performResult : 0);
        if (failure < 0 || sink.PostDeleteCount == 0)
        {
            if (failure is HResultFileNotFound or HResultPathNotFound)
            {
                return new EntryRemoveResult(EntryRemoveStatus.NotFound, "The folder does not exist any more.");
            }

            return Failed($"Windows could not remove it, it is probably in use (0x{failure:X8}).");
        }

        // The shell said it worked: confirm that the folder is really gone before reporting success.
        if (_probe.Probe(path).Status != PathProbeStatus.NotFound)
        {
            return Failed("Windows reported success but the folder is still there.");
        }

        return new EntryRemoveResult(EntryRemoveStatus.Removed, DeletedPermanentlyUnexpectedly: sink.UnexpectedPermanentDelete && mode == CleanupMode.RecycleBin);
    }

    private static EntryRemoveResult Failed(string reason) => new(EntryRemoveStatus.Failed, reason);

    private static unsafe nint* VTable(nint instance) => *(nint**)instance;

    private static unsafe void Release(nint instance)
    {
        if (instance != 0)
        {
            ((delegate* unmanaged[Stdcall]<nint, uint>)VTable(instance)[2])(instance);
        }
    }
}

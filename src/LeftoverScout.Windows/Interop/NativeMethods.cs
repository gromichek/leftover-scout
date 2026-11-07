using System.Runtime.InteropServices;

namespace LeftoverScout.Windows.Interop;

/// <summary>
/// Native functions used by the Windows layer.
/// </summary>
internal static unsafe partial class NativeMethods
{
    /// <summary>FindExInfoBasic: skips the short (8.3) name, which is faster.</summary>
    internal const int FindExInfoBasic = 1;

    /// <summary>FindExSearchNameMatch.</summary>
    internal const int FindExSearchNameMatch = 0;

    /// <summary>FIND_FIRST_EX_LARGE_FETCH.</summary>
    internal const int FindFirstExLargeFetch = 2;

    internal const int ErrorFileNotFound = 2;
    internal const int ErrorPathNotFound = 3;
    internal const int ErrorAccessDenied = 5;
    internal const int ErrorNoMoreFiles = 18;

    [LibraryImport("kernel32.dll", EntryPoint = "FindFirstFileExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial FindHandle FindFirstFileEx(
        string fileName,
        int infoLevelId,
        ref Win32FindData findFileData,
        int searchOp,
        nint searchFilter,
        int additionalFlags);

    [LibraryImport("kernel32.dll", EntryPoint = "FindNextFileW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FindNextFile(FindHandle findFile, ref Win32FindData findFileData);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool FindClose(nint findFile);

    [LibraryImport("shell32.dll", EntryPoint = "SHGetKnownFolderPath")]
    internal static partial int SHGetKnownFolderPath(in Guid folderId, uint flags, nint token, out nint path);

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplayMonitors")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(
        nint hdc,
        nint clipRect,
        delegate* unmanaged[Stdcall]<nint, nint, Rect*, nint, int> callback,
        nint data);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
}

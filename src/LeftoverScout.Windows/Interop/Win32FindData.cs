using System.Runtime.InteropServices;

namespace LeftoverScout.Windows.Interop;

/// <summary>
/// WIN32_FIND_DATAW. <see cref="DwReserved0"/> holds the reparse tag when the reparse point attribute is set.
/// FILETIME is two 32-bit values in the native struct, so it is 4-byte aligned: Pack = 4 keeps the 64-bit
/// time fields at the native offsets (total size 592 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
internal unsafe struct Win32FindData
{
    public uint DwFileAttributes;
    public ulong FtCreationTime;
    public ulong FtLastAccessTime;
    public ulong FtLastWriteTime;
    public uint NFileSizeHigh;
    public uint NFileSizeLow;
    public uint DwReserved0;
    public uint DwReserved1;
    public fixed char CFileName[260];
    public fixed char CAlternateFileName[14];
}

using System.Runtime.InteropServices;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Windows.Interop;
using LeftoverScout.Windows.Scanning;

namespace LeftoverScout.Windows.Cleanup;

/// <summary>
/// The live file system queries of the cleanup guard. It asks only for attributes of single paths:
/// FindFirstFileExW on the item itself (reports a link as a link, never follows it, opens no file) and
/// GetLongPathNameW (expands 8.3 short names). Paths get the \\?\ prefix so that paths longer than 260 characters
/// work ("Naming Files, Paths, and Namespaces", Microsoft Learn). Nothing is written, moved or deleted.
/// </summary>
public sealed class WindowsPathProbe : IPathProbe
{
    private const string ExtendedPrefix = @"\\?\";
    private const string ExtendedUncPrefix = @"\\?\UNC\";

    /// <inheritdoc />
    public PathProbe Probe(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // FindFirstFileExW would treat wildcards as a search pattern; the guard rejects them earlier, this is a second line.
        if (path.AsSpan().IndexOfAny('*', '?') >= 0)
        {
            return PathProbe.Failed;
        }

        var data = default(Win32FindData);
        using FindHandle handle = NativeMethods.FindFirstFileEx(
            Win32DirectoryReader.ToExtendedPath(path),
            NativeMethods.FindExInfoBasic,
            ref data,
            NativeMethods.FindExSearchNameMatch,
            0,
            0);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            return error is NativeMethods.ErrorFileNotFound or NativeMethods.ErrorPathNotFound
                ? PathProbe.NotFound
                : PathProbe.Failed;
        }

        uint attributes = data.DwFileAttributes;
        uint tag = (attributes & EntryClassifier.AttributeReparsePoint) != 0 ? data.DwReserved0 : 0;
        return new PathProbe(PathProbeStatus.Found, attributes, tag);
    }

    /// <inheritdoc />
    public unsafe string? GetLongPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string extended = Win32DirectoryReader.ToExtendedPath(path);
        uint size = 1024;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            char[] buffer = new char[size];
            uint length;
            fixed (char* start = buffer)
            {
                length = NativeMethods.GetLongPathName(extended, start, size);
            }

            if (length == 0)
            {
                return null;
            }

            if (length < size)
            {
                return StripExtendedPrefix(new string(buffer, 0, (int)length));
            }

            // The buffer was too small; the return value is the required size including the terminator.
            size = length + 1;
        }

        return null;
    }

    private static string StripExtendedPrefix(string path)
    {
        if (path.StartsWith(ExtendedUncPrefix, StringComparison.Ordinal))
        {
            return @"\\" + path[ExtendedUncPrefix.Length..];
        }

        return path.StartsWith(ExtendedPrefix, StringComparison.Ordinal) ? path[ExtendedPrefix.Length..] : path;
    }
}

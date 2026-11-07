using System.ComponentModel;
using System.Runtime.InteropServices;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Windows.Interop;

namespace LeftoverScout.Windows.Scanning;

/// <summary>
/// Lists a folder with FindFirstFileExW / FindNextFileW. It only enumerates: no file is opened, no file data is
/// read and nothing is changed. Paths get the \\?\ prefix so that paths longer than 260 characters work
/// ("Naming Files, Paths, and Namespaces", Microsoft Learn).
/// </summary>
public sealed class Win32DirectoryReader : IDirectoryReader
{
    private const uint ReparsePointAttribute = 0x00000400;
    private const uint DirectoryAttribute = 0x00000010;

    /// <inheritdoc />
    public DirectoryListing ReadDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var entries = new List<DirectoryEntry>();
        var data = default(Win32FindData);

        using FindHandle handle = NativeMethods.FindFirstFileEx(
            ToExtendedPath(path) + @"\*",
            NativeMethods.FindExInfoBasic,
            ref data,
            NativeMethods.FindExSearchNameMatch,
            0,
            NativeMethods.FindFirstExLargeFetch);

        if (handle.IsInvalid)
        {
            return CreateFailure(Marshal.GetLastPInvokeError());
        }

        while (true)
        {
            AddEntry(entries, ref data);

            if (!NativeMethods.FindNextFile(handle, ref data))
            {
                int error = Marshal.GetLastPInvokeError();
                return error == NativeMethods.ErrorNoMoreFiles
                    ? DirectoryListing.Success(entries)
                    : CreateFailureWithEntries(entries, error);
            }
        }
    }

    /// <summary>Converts a path to the extended-length form (\\?\C:\... or \\?\UNC\server\share\...).</summary>
    internal static string ToExtendedPath(string path)
    {
        string trimmed = path.Length > 3 ? path.TrimEnd('\\') : path;
        if (trimmed.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return trimmed.StartsWith(@"\\", StringComparison.Ordinal)
            ? @"\\?\UNC\" + trimmed[2..]
            : @"\\?\" + trimmed;
    }

    private static unsafe void AddEntry(List<DirectoryEntry> entries, ref Win32FindData data)
    {
        string name;
        fixed (char* nameStart = data.CFileName)
        {
            name = new string(nameStart);
        }

        if (name is "." or "..")
        {
            return;
        }

        uint attributes = data.DwFileAttributes;
        bool isReparsePoint = (attributes & ReparsePointAttribute) != 0;
        long length = ((long)data.NFileSizeHigh << 32) | data.NFileSizeLow;

        entries.Add(new DirectoryEntry(
            name,
            (attributes & DirectoryAttribute) != 0,
            length,
            ToUtc(data.FtLastWriteTime),
            attributes,
            isReparsePoint ? data.DwReserved0 : 0));
    }

    private static DateTime ToUtc(ulong fileTime)
    {
        try
        {
            return DateTime.FromFileTimeUtc((long)fileTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    private static DirectoryListing CreateFailure(int error) =>
        DirectoryListing.Failure(Describe(error), error == NativeMethods.ErrorAccessDenied);

    private static DirectoryListing CreateFailureWithEntries(List<DirectoryEntry> entries, int error) =>
        new(entries, Describe(error), error == NativeMethods.ErrorAccessDenied);

    private static string Describe(int error) =>
        error == NativeMethods.ErrorAccessDenied
            ? "Access denied"
            : $"{new Win32Exception(error).Message} (error {error})";
}

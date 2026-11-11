using LeftoverScout.Core.Cleanup;
using LeftoverScout.Windows.Interop;
using Microsoft.Win32;

namespace LeftoverScout.Windows.Cleanup;

/// <summary>
/// Reads the Recycle Bin settings of a drive for the current user (D-062 layer 1), read-only:
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume\{volume GUID}</c> values
/// <c>NukeOnDelete</c> (1 = files are deleted without the bin) and <c>MaxCapacity</c> (MB), and the policy
/// <c>Policies\Explorer\NoRecycleFiles</c> ("Do not move deleted files to the Recycle Bin") in HKCU and HKLM.
/// Anything that cannot be read gives <see cref="RecycleBinSettings.Unknown"/>; layers 2 and 3 still protect.
/// </summary>
public sealed class WindowsRecycleBinSettingsReader : IRecycleBinSettingsReader
{
    private const string BinVolumesKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\BitBucket\Volume";
    private const string PolicyKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";

    /// <inheritdoc />
    public RecycleBinSettings Read(string path)
    {
        try
        {
            if (IsPolicySet(Registry.CurrentUser) || IsPolicySet(Registry.LocalMachine))
            {
                return new RecycleBinSettings(true, null);
            }

            string? guid = GetVolumeKeyName(path);
            if (guid is null)
            {
                return RecycleBinSettings.Unknown;
            }

            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(BinVolumesKey + "\\" + guid, writable: false);
            if (key is null)
            {
                return RecycleBinSettings.Unknown;
            }

            bool off = key.GetValue("NukeOnDelete") is int nuke && nuke != 0;
            long? max = key.GetValue("MaxCapacity") is int megabytes && megabytes > 0 ? megabytes * 1024L * 1024L : null;
            return new RecycleBinSettings(off, max);
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or IOException or ArgumentException)
        {
            return RecycleBinSettings.Unknown;
        }
    }

    private static bool IsPolicySet(RegistryKey hive)
    {
        using RegistryKey? key = hive.OpenSubKey(PolicyKey, writable: false);
        return key?.GetValue("NoRecycleFiles") is int value && value != 0;
    }

    // "\\?\Volume{guid}\" -> "{guid}"
    private static unsafe string? GetVolumeKeyName(string path)
    {
        string? root = System.IO.Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root) || root.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return null;
        }

        char[] buffer = new char[64];
        bool ok;
        fixed (char* start = buffer)
        {
            ok = NativeMethods.GetVolumeNameForVolumeMountPoint(root, start, (uint)buffer.Length);
        }

        if (!ok)
        {
            return null;
        }

        string name = new string(buffer).TrimEnd('\0');
        int open = name.IndexOf('{', StringComparison.Ordinal);
        int close = name.IndexOf('}', StringComparison.Ordinal);
        return open >= 0 && close > open ? name[open..(close + 1)] : null;
    }
}

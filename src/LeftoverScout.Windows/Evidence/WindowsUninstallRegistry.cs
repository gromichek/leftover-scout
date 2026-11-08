using System.Security;
using LeftoverScout.Core.Evidence;
using Microsoft.Win32;

namespace LeftoverScout.Windows.Evidence;

/// <summary>
/// Reads the Uninstall registry keys. Keys are opened read-only (<c>writable: false</c>); nothing is ever written
/// (decision D-002). Docs: <c>RegistryKey.OpenBaseKey</c>, <c>RegistryView</c>, <c>RegistryKey.OpenSubKey</c> on Microsoft Learn.
/// </summary>
public sealed class WindowsUninstallRegistry : IUninstallRegistry
{
    /// <inheritdoc />
    public IReadOnlyList<UninstallEntry> ReadEntries(UninstallLocation location, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(location);

        RegistryHive hive = location.Hive == UninstallHive.LocalMachine ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        RegistryView view = location.View == UninstallView.Registry64 ? RegistryView.Registry64 : RegistryView.Registry32;

        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
        using RegistryKey? uninstall = baseKey.OpenSubKey(UninstallLocation.KeyPath, writable: false);
        if (uninstall is null)
        {
            return [];
        }

        var entries = new List<UninstallEntry>();
        foreach (string name in uninstall.GetSubKeyNames())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using RegistryKey? key = uninstall.OpenSubKey(name, writable: false);
                if (key is not null)
                {
                    entries.Add(Read(name, key));
                }
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException or ObjectDisposedException)
            {
                // One unreadable subkey must not hide the others.
            }
        }

        return entries;
    }

    private static UninstallEntry Read(string name, RegistryKey key) =>
        new(
            name,
            Text(key, "DisplayName"),
            Text(key, "Publisher"),
            Text(key, "DisplayVersion"),
            Text(key, "InstallLocation"),
            Text(key, "DisplayIcon"),
            Text(key, "InstallDate"),
            Text(key, "ParentKeyName"),
            Text(key, "ReleaseType"),
            key.GetValue("SystemComponent") is int systemComponent ? systemComponent : null);

    // Text values only; a number (for example InstallDate stored as DWORD) is converted, anything else is ignored.
    private static string? Text(RegistryKey key, string valueName) => key.GetValue(valueName) switch
    {
        string text => text,
        int number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => null,
    };
}

namespace LeftoverScout.Core.Evidence;

/// <summary>
/// One of the four Uninstall keys that are read: HKLM and HKCU, each in the 64-bit and 32-bit view.
/// </summary>
/// <param name="Hive">The registry hive.</param>
/// <param name="View">The registry view.</param>
public sealed record UninstallLocation(UninstallHive Hive, UninstallView View)
{
    /// <summary>Path of the Uninstall key below the hive.</summary>
    public const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    /// <summary>The four locations in reading order (64-bit views first, so duplicates keep the 64-bit entry).</summary>
    public static IReadOnlyList<UninstallLocation> All { get; } =
    [
        new(UninstallHive.LocalMachine, UninstallView.Registry64),
        new(UninstallHive.LocalMachine, UninstallView.Registry32),
        new(UninstallHive.CurrentUser, UninstallView.Registry64),
        new(UninstallHive.CurrentUser, UninstallView.Registry32),
    ];

    /// <summary>Describes the key of one program, for example <c>HKLM\SOFTWARE\...\Uninstall\Name (64-bit view)</c>.</summary>
    public string Describe(string subKeyName) =>
        $@"{(Hive == UninstallHive.LocalMachine ? "HKLM" : "HKCU")}\{KeyPath}\{subKeyName} ({(View == UninstallView.Registry64 ? "64-bit" : "32-bit")} view)";
}

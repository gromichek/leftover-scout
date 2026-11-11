namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// A hard-coded list of folders below the profile root that must never be deleted, whatever the rule files say.
/// It repeats the Windows security folders of <c>rules/protected.json</c> in code, so that a missing or broken rule
/// file can never unprotect them. A folder is blocked when it is on the list, lies inside a subtree entry, or
/// contains an entry.
/// </summary>
internal static class ProtectedDenyList
{
    /// <summary>The profile root and the fixed AppData folders (only these exact folders).</summary>
    public static IReadOnlyList<string[]> FixedRoots { get; } =
    [
        [],
        ["AppData"],
        ["AppData", "Roaming"],
        ["AppData", "Local"],
        ["AppData", "LocalLow"],
    ];

    private static readonly (string[] Segments, bool Subtree)[] Entries =
    [
        (["AppData", "Roaming", "Microsoft"], false),
        (["AppData", "Local", "Microsoft"], false),
        (["AppData", "Local", "Packages"], false),
        (["AppData", "Local", "Programs"], false),
        (["AppData", "Roaming", "Microsoft", "Protect"], true),
        (["AppData", "Roaming", "Microsoft", "Credentials"], true),
        (["AppData", "Local", "Microsoft", "Credentials"], true),
        (["AppData", "Roaming", "Microsoft", "SystemCertificates"], true),
        (["AppData", "Roaming", "Microsoft", "Crypto"], true),
        (["AppData", "Local", "Microsoft", "Vault"], true),
        (["AppData", "Roaming", "Microsoft", "Windows", "Start Menu"], true),
        (["AppData", "Local", "Microsoft", "WindowsApps"], true),
    ];

    /// <summary>True when the profile-relative folder is one of the fixed roots.</summary>
    public static bool IsFixedRoot(IReadOnlyList<string> relative) =>
        FixedRoots.Any(root => root.Length == relative.Count && ParsedPath.StartsWith(relative, root));

    /// <summary>True when the profile-relative folder is on the deny list, inside a subtree entry, or contains an entry.</summary>
    public static bool Blocks(IReadOnlyList<string> relative)
    {
        foreach ((string[] segments, bool subtree) in Entries)
        {
            if (ParsedPath.StartsWith(relative, segments))
            {
                // Equal to the entry, or inside it: blocked for a subtree entry, and for a folder entry only when equal.
                if (subtree || relative.Count == segments.Length)
                {
                    return true;
                }
            }

            // The folder contains the entry: deleting it would delete the entry.
            if (relative.Count < segments.Length && ParsedPath.StartsWith(segments, relative))
            {
                return true;
            }
        }

        return false;
    }
}

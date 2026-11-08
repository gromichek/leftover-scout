using System.Globalization;

namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Pure rules that turn Uninstall registry entries into <see cref="InstalledProgram"/> values (task T-004, section 2).
/// Registry value names: "Uninstall Registry Key", Microsoft Learn, and the Windows Installer property reference.
/// </summary>
public static class UninstallEntryParser
{
    private static readonly string[] UpdateReleaseTypes = ["Update", "Hotfix", "Security Update"];

    /// <summary>
    /// Parses one entry. Returns null for entries that are not programs: no display name, or an update/patch.
    /// </summary>
    public static InstalledProgram? Parse(UninstallLocation location, UninstallEntry entry)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.DisplayName) || IsUpdate(entry))
        {
            return null;
        }

        string? installLocation = Clean(entry.InstallLocation) ?? FolderOfIcon(entry.DisplayIcon);

        return new InstalledProgram(
            entry.DisplayName.Trim(),
            ProgramSource.Registry,
            location.Describe(entry.KeyName),
            Clean(entry.Publisher),
            Clean(entry.DisplayVersion),
            installLocation,
            ParseInstallDate(entry.InstallDate),
            IsSystemComponent: entry.SystemComponent == 1);
    }

    /// <summary>
    /// Parses all entries of all locations. The same program found in both views of one hive (same key name and
    /// display name) is listed once; the first one (64-bit view) is kept.
    /// </summary>
    public static IReadOnlyList<InstalledProgram> ParseAll(IEnumerable<(UninstallLocation Location, IReadOnlyList<UninstallEntry> Entries)> locations)
    {
        ArgumentNullException.ThrowIfNull(locations);

        var programs = new List<InstalledProgram>();
        var seen = new HashSet<(UninstallHive, string, string)>();

        foreach ((UninstallLocation location, IReadOnlyList<UninstallEntry> entries) in locations)
        {
            foreach (UninstallEntry entry in entries)
            {
                InstalledProgram? program = Parse(location, entry);
                if (program is null)
                {
                    continue;
                }

                var key = (location.Hive, entry.KeyName.ToUpperInvariant(), program.Name.ToUpperInvariant());
                if (seen.Add(key))
                {
                    programs.Add(program);
                }
            }
        }

        return programs;
    }

    /// <summary>Parses an InstallDate value: exactly <c>yyyyMMdd</c>, anything else gives null.</summary>
    public static DateOnly? ParseInstallDate(string? value) =>
        value is not null && DateOnly.TryParseExact(value.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : null;

    /// <summary>
    /// Returns the folder of a DisplayIcon value such as <c>"C:\Apps\Sample\sample.exe",0</c> (quotes and the
    /// <c>,index</c> suffix are removed); null when no folder can be derived.
    /// </summary>
    public static string? FolderOfIcon(string? displayIcon)
    {
        // Not Clean(): the quotes surround only the file name, and the index follows the closing quote.
        string? text = displayIcon?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (text[0] == '"')
        {
            int close = text.IndexOf('"', 1);
            text = close > 0 ? text[1..close] : text[1..];
        }
        else
        {
            int comma = text.LastIndexOf(',');
            if (comma > 0 && int.TryParse(text.AsSpan(comma + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
            {
                text = text[..comma];
            }
        }

        text = text.Trim();
        int separator = text.LastIndexOfAny(['\\', '/']);
        if (separator <= 0)
        {
            return null;
        }

        string folder = text[..separator];

        // A bare drive ("C:") is not an install folder.
        return folder.EndsWith(':') ? null : folder;
    }

    private static bool IsUpdate(UninstallEntry entry) =>
        !string.IsNullOrWhiteSpace(entry.ParentKeyName)
        || (entry.ReleaseType is { } releaseType && UpdateReleaseTypes.Contains(releaseType.Trim(), StringComparer.OrdinalIgnoreCase));

    private static string? Clean(string? value)
    {
        string? trimmed = value?.Trim().Trim('"').Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Decides which folders are personal (D-020, D-049). A folder is personal when it is one of the known personal folder
/// paths inside the profile, or when it is a direct child of the profile root with a standard personal name
/// (see <see cref="PersonalNames"/>). The name rule keeps <c>{Profile}\Documents</c> personal even when Windows has
/// redirected the real Documents folder to another drive.
/// </summary>
public sealed class PersonalFolderMatcher
{
    /// <summary>Standard personal folder names in the profile root (decision D-049), compared case-insensitively.</summary>
    public static IReadOnlyList<string> PersonalNames { get; } =
    [
        "Documents", "Desktop", "Downloads", "Pictures", "Videos", "Music", "Favorites", "Contacts", "Links",
        "Saved Games", "Searches", "3D Objects",
    ];

    /// <summary>Folders whose name starts with this text are personal (<c>OneDrive</c>, <c>OneDrive - Organization</c>).</summary>
    public const string OneDrivePrefix = "OneDrive";

    private readonly string _profileRoot;
    private readonly HashSet<string> _personalPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a matcher.</summary>
    /// <param name="profileRootPath">Profile root without trailing separator.</param>
    /// <param name="personalFolderPaths">Candidate personal folder paths; invalid or outside paths are ignored.</param>
    public PersonalFolderMatcher(string profileRootPath, IEnumerable<string> personalFolderPaths)
    {
        ArgumentNullException.ThrowIfNull(personalFolderPaths);

        string root = Path.TrimEndingDirectorySeparator(profileRootPath);
        _profileRoot = root;
        string rootWithSeparator = root + Path.DirectorySeparatorChar;

        foreach (string candidate in personalFolderPaths)
        {
            if (string.IsNullOrWhiteSpace(candidate) || !Path.IsPathFullyQualified(candidate))
            {
                continue;
            }

            string trimmed = Path.TrimEndingDirectorySeparator(candidate);
            if (trimmed.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                _personalPaths.Add(trimmed);
            }
        }
    }

    /// <summary>Returns true when <paramref name="folderPath"/> is one of the personal folders.</summary>
    public bool IsPersonal(string folderPath)
    {
        string trimmed = Path.TrimEndingDirectorySeparator(folderPath);
        return _personalPaths.Contains(trimmed) || IsPersonalByName(trimmed);
    }

    // Direct child of the profile root with a standard personal name (decision D-049).
    private bool IsPersonalByName(string trimmedPath)
    {
        if (!string.Equals(Path.GetDirectoryName(trimmedPath), _profileRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string name = Path.GetFileName(trimmedPath);
        return name.StartsWith(OneDrivePrefix, StringComparison.OrdinalIgnoreCase)
            || PersonalNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }
}

namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Decides which folders are personal (D-020). Only folders inside the profile are considered.
/// </summary>
public sealed class PersonalFolderMatcher
{
    private readonly HashSet<string> _personalPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a matcher.</summary>
    /// <param name="profileRootPath">Profile root without trailing separator.</param>
    /// <param name="personalFolderPaths">Candidate personal folder paths; invalid or outside paths are ignored.</param>
    public PersonalFolderMatcher(string profileRootPath, IEnumerable<string> personalFolderPaths)
    {
        ArgumentNullException.ThrowIfNull(personalFolderPaths);

        string root = Path.TrimEndingDirectorySeparator(profileRootPath);
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
    public bool IsPersonal(string folderPath) => _personalPaths.Contains(Path.TrimEndingDirectorySeparator(folderPath));
}

using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Evidence from the Start menu (decision D-035, source 4): the names of shortcuts (<c>.lnk</c>, <c>.url</c>,
/// without extension) and folders. Shortcut files are never opened; only the folder listing is read.
/// A folder whose name equals a shortcut name is not listed twice.
/// </summary>
public sealed class StartMenuSource : IInstalledProgramSource
{
    private static readonly string[] ShortcutExtensions = [".lnk", ".url"];

    private readonly IDirectoryReader _reader;
    private readonly IEvidenceLocations _locations;

    /// <summary>Creates the source.</summary>
    public StartMenuSource(IDirectoryReader reader, IEvidenceLocations locations)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(locations);
        _reader = reader;
        _locations = locations;
    }

    /// <inheritdoc />
    public ProgramSource Source => ProgramSource.StartMenu;

    /// <inheritdoc />
    public IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken)
    {
        var shortcuts = new List<InstalledProgram>();
        var folders = new List<InstalledProgram>();
        var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in _locations.GetStartMenuRoots())
        {
            if (string.IsNullOrWhiteSpace(root) || !seenRoots.Add(root))
            {
                continue;
            }

            // Explicit stack instead of recursion; the second value is the path relative to the root.
            var pending = new Stack<(string Path, string Relative)>();
            pending.Push((root, string.Empty));

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (string path, string relative) = pending.Pop();

                foreach (DirectoryEntry entry in _reader.ReadDirectory(path).Entries)
                {
                    EntryKind kind = EntryClassifier.Classify(entry);
                    string entryRelative = relative.Length == 0 ? entry.Name : relative + "\\" + entry.Name;

                    if (kind == EntryKind.Directory)
                    {
                        folders.Add(new InstalledProgram(entry.Name, ProgramSource.StartMenu, entryRelative));
                        pending.Push((System.IO.Path.Combine(path, entry.Name), entryRelative));
                    }
                    else if ((kind is EntryKind.File or EntryKind.CloudOnlyFile) && IsShortcut(entry.Name))
                    {
                        shortcuts.Add(new InstalledProgram(
                            System.IO.Path.GetFileNameWithoutExtension(entry.Name), ProgramSource.StartMenu, entryRelative));
                    }
                }
            }
        }

        var shortcutNames = new HashSet<string>(shortcuts.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
        var result = new List<InstalledProgram>(shortcuts);
        result.AddRange(folders.Where(folder => !shortcutNames.Contains(folder.Name)));
        return result;
    }

    private static bool IsShortcut(string fileName) =>
        ShortcutExtensions.Contains(System.IO.Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
}

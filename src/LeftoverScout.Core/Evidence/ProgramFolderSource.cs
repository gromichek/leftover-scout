using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Evidence from the top-level folders of Program Files, Program Files (x86) and the per-user Programs folder
/// (decision D-035, source 3). The names of the direct subfolders are kept for matching. Only folder listings are
/// read; links and cloud folders are not entered.
/// </summary>
public sealed class ProgramFolderSource : IInstalledProgramSource
{
    private readonly IDirectoryReader _reader;
    private readonly IEvidenceLocations _locations;

    /// <summary>Creates the source.</summary>
    public ProgramFolderSource(IDirectoryReader reader, IEvidenceLocations locations)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(locations);
        _reader = reader;
        _locations = locations;
    }

    /// <inheritdoc />
    public ProgramSource Source => ProgramSource.ProgramFolder;

    /// <inheritdoc />
    public IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken)
    {
        var programs = new List<InstalledProgram>();
        var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in _locations.GetProgramFolderRoots())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(root) || !seenRoots.Add(root))
            {
                continue;
            }

            // A root that cannot be listed (for example it does not exist) simply gives nothing.
            foreach (DirectoryEntry folder in Folders(_reader.ReadDirectory(root)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string path = System.IO.Path.Combine(root, folder.Name);
                string[] subfolders = Folders(_reader.ReadDirectory(path)).Select(entry => entry.Name).ToArray();
                programs.Add(new InstalledProgram(folder.Name, ProgramSource.ProgramFolder, path, InstallLocation: path)
                {
                    SubfolderNames = subfolders,
                });
            }
        }

        return programs;
    }

    internal static IEnumerable<DirectoryEntry> Folders(DirectoryListing listing) =>
        listing.Entries.Where(entry => EntryClassifier.Classify(entry) == EntryKind.Directory);
}

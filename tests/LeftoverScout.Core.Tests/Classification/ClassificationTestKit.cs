using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Rules;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Classification;

/// <summary>A clock that always returns the same moment.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>Builds a fictional profile folder by folder (paths relative to the profile root).</summary>
internal sealed class TreeBuilder
{
    private readonly Dictionary<string, List<DirectoryEntry>> _folders = new(StringComparer.OrdinalIgnoreCase) { [Root] = [] };
    private readonly List<string> _denied = [];

    public TreeBuilder Dir(string relativePath)
    {
        Ensure(Root + "\\" + relativePath);
        return this;
    }

    public TreeBuilder File(string folderRelativePath, string name, DateTime lastWriteUtc, long length = 10)
    {
        string path = Ensure(Root + "\\" + folderRelativePath);
        _folders[path].Add(new DirectoryEntry(name, false, length, lastWriteUtc, 0x20, 0));
        return this;
    }

    public TreeBuilder Raw(string folderRelativePath, DirectoryEntry entry)
    {
        string path = folderRelativePath.Length == 0 ? Root : Ensure(Root + "\\" + folderRelativePath);
        _folders[path].Add(entry);
        return this;
    }

    public TreeBuilder Denied(string relativePath)
    {
        _denied.Add(Ensure(Root + "\\" + relativePath));
        return this;
    }

    public FolderNode Build(IReadOnlyList<string>? personalFolders = null)
    {
        var reader = new FakeDirectoryReader();
        foreach ((string path, List<DirectoryEntry> entries) in _folders)
        {
            reader.Folder(path, [.. entries]);
        }

        foreach (string path in _denied)
        {
            reader.Failing(path, "Access denied", accessDenied: true);
        }

        return RuleTestHelper.Scan(reader, personalFolders);
    }

    private string Ensure(string path)
    {
        if (_folders.ContainsKey(path))
        {
            return path;
        }

        int separator = path.LastIndexOf('\\');
        string parent = Ensure(path[..separator]);
        _folders[parent].Add(DirEntry(path[(separator + 1)..]));
        _folders[path] = [];
        return path;
    }
}

internal static class Evidences
{
    public static InstalledProgram Registry(string name, string? publisher = null, string key = "{11111111-2222-3333-4444-555555555555}") =>
        new(name, ProgramSource.Registry, $@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{key} (64-bit view)", publisher);

    public static InstalledProgram Store(string name, string family, string? publisher = null) =>
        new(name, ProgramSource.Store, family, publisher);

    public static InstalledProgram ProgramFolder(string name, params string[] subfolders) =>
        new(name, ProgramSource.ProgramFolder, @"C:\Program Files\" + name) { SubfolderNames = subfolders };

    public static InstalledProgram StartMenu(string name) =>
        new(name, ProgramSource.StartMenu, name + ".lnk");

    public static EvidenceSnapshot Snapshot(params InstalledProgram[] programs) =>
        new(programs, new Dictionary<ProgramSource, int>(), [], DateTimeOffset.UnixEpoch, TimeSpan.Zero);

    public static EvidenceSnapshot SnapshotWithError(params InstalledProgram[] programs) =>
        new(programs, new Dictionary<ProgramSource, int>(), [new EvidenceError(ProgramSource.Store, "Store apps could not be read.")], DateTimeOffset.UnixEpoch, TimeSpan.Zero);
}

using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Scanning;

/// <summary>
/// An in-memory file system for scanner tests. Paths are matched ignoring case.
/// </summary>
public sealed class FakeDirectoryReader : IDirectoryReader
{
    public const string Root = @"C:\Users\SampleUser";

    private readonly Dictionary<string, DirectoryListing> _folders = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ReadPaths { get; } = [];

    /// <summary>Called after each folder is read (for example to advance a fake clock or cancel).</summary>
    public Action<string>? OnRead { get; set; }

    public FakeDirectoryReader Folder(string path, params DirectoryEntry[] entries)
    {
        _folders[path] = DirectoryListing.Success(entries);
        return this;
    }

    public FakeDirectoryReader Failing(string path, string reason, bool accessDenied)
    {
        _folders[path] = DirectoryListing.Failure(reason, accessDenied);
        return this;
    }

    public DirectoryListing ReadDirectory(string path)
    {
        ReadPaths.Add(path);
        OnRead?.Invoke(path);
        return _folders.TryGetValue(path, out DirectoryListing? listing) ? listing : DirectoryListing.Success([]);
    }

    public static DirectoryEntry FileEntry(string name, long length, int year = 2024) =>
        new(name, false, length, new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0x20, 0);

    public static DirectoryEntry CloudFileEntry(string name, long length) =>
        new(name, false, length, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 0x20 | EntryClassifier.AttributeRecallOnDataAccess | EntryClassifier.AttributeReparsePoint, 0x9000701A);

    public static DirectoryEntry DirEntry(string name) =>
        new(name, true, 0, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), EntryClassifier.AttributeDirectory, 0);

    public static DirectoryEntry CloudDirEntry(string name) =>
        new(name, true, 0, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), EntryClassifier.AttributeDirectory | EntryClassifier.AttributeRecallOnOpen, 0);

    public static DirectoryEntry JunctionEntry(string name) =>
        new(name, true, 0, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), EntryClassifier.AttributeDirectory | EntryClassifier.AttributeReparsePoint, EntryClassifier.ReparseTagMountPoint);
}

using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;

namespace LeftoverScout.Core.Tests.Cleanup;

/// <summary>An in-memory file system for guard tests. Paths are matched ignoring case; unknown paths do not exist.</summary>
internal sealed class FakePathProbe : IPathProbe
{
    private readonly Dictionary<string, PathProbe> _paths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string?> _longPaths = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Probed { get; } = [];

    public List<string> LongPathRequests { get; } = [];

    /// <summary>Adds a folder and every parent below the profile root as normal folders.</summary>
    public FakePathProbe Folder(string path)
    {
        string current = path.TrimEnd('\\');
        while (current.Length > FakeDirectoryReader.Root.Length && current.StartsWith(FakeDirectoryReader.Root, StringComparison.OrdinalIgnoreCase))
        {
            _paths.TryAdd(current, new PathProbe(PathProbeStatus.Found, EntryClassifier.AttributeDirectory));
            current = current[..current.LastIndexOf('\\')];
        }

        return this;
    }

    public FakePathProbe Set(string path, PathProbe probe)
    {
        _paths[path] = probe;
        return this;
    }

    public FakePathProbe Junction(string path) =>
        Set(path, new PathProbe(PathProbeStatus.Found, EntryClassifier.AttributeDirectory | EntryClassifier.AttributeReparsePoint, EntryClassifier.ReparseTagMountPoint));

    public FakePathProbe LongPath(string path, string? longPath)
    {
        _longPaths[path] = longPath;
        return this;
    }

    public PathProbe Probe(string path)
    {
        Probed.Add(path);
        return _paths.TryGetValue(path, out PathProbe? probe) ? probe : PathProbe.NotFound;
    }

    public string? GetLongPath(string path)
    {
        LongPathRequests.Add(path);
        return _longPaths.TryGetValue(path, out string? longPath) ? longPath : path;
    }
}

using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// Builds the rows of the Large files table (decision D-033).
/// </summary>
public static class LargeFileRowBuilder
{
    /// <summary>Builds the rows, largest first, then by path.</summary>
    /// <param name="largeFiles">Large files of a scan result.</param>
    /// <param name="profileRoot">Profile root path, used for the relative path.</param>
    public static IReadOnlyList<LargeFileRow> Build(IEnumerable<LargeFile> largeFiles, string profileRoot)
    {
        ArgumentNullException.ThrowIfNull(largeFiles);
        ArgumentNullException.ThrowIfNull(profileRoot);

        return largeFiles
            .Select(file => new LargeFileRow(RelativePath(file.Path, profileRoot), file.Path, file.Size, file.LastWriteTimeUtc))
            .OrderByDescending(row => row.Size)
            .ThenBy(row => row.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The path relative to <paramref name="profileRoot"/>; the full path when it is not inside the root.
    /// </summary>
    public static string RelativePath(string fullPath, string profileRoot)
    {
        string root = profileRoot.TrimEnd('\\', '/');
        if (root.Length > 0
            && fullPath.Length > root.Length + 1
            && fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            && (fullPath[root.Length] == '\\' || fullPath[root.Length] == '/'))
        {
            return fullPath[(root.Length + 1)..];
        }

        return fullPath;
    }
}

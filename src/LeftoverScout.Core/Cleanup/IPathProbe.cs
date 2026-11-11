namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Asks the file system about single paths for the cleanup guard. Implementations only query attributes: they never
/// open a file, never follow a link, never read file data and never change anything.
/// </summary>
public interface IPathProbe
{
    /// <summary>Reads the attributes of the item itself (a link is not followed).</summary>
    /// <param name="path">Fully qualified path.</param>
    PathProbe Probe(string path);

    /// <summary>
    /// Returns the long form of the path (8.3 short names expanded, real letter case), or null when it cannot be
    /// determined. The path must exist.
    /// </summary>
    /// <param name="path">Fully qualified path.</param>
    string? GetLongPath(string path);
}

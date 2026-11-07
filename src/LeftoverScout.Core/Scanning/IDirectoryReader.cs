namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Lists the entries of one folder. Implementations only enumerate: they never open files, read file data
/// or change anything.
/// </summary>
public interface IDirectoryReader
{
    /// <summary>
    /// Lists the direct entries of <paramref name="path"/> (without "." and ".."). Errors are returned in the
    /// listing, not thrown.
    /// </summary>
    /// <param name="path">Fully qualified folder path.</param>
    DirectoryListing ReadDirectory(string path);
}

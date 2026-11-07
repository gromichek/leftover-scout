namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Supplies the paths of the user's personal folders (Documents, Pictures, OneDrive, ...), decision D-020.
/// </summary>
public interface IPersonalFolderProvider
{
    /// <summary>Returns the fully qualified paths of the personal folders. May contain duplicates or missing folders.</summary>
    IReadOnlyList<string> GetPersonalFolderPaths();
}

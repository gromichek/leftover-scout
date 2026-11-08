namespace LeftoverScout.Core.Shell;

/// <summary>
/// Checks whether a folder exists. Read-only; it never opens or enumerates the folder.
/// </summary>
public interface IFolderExistence
{
    /// <summary>Returns true when <paramref name="folderPath"/> is an existing folder. Never throws.</summary>
    bool FolderExists(string folderPath);
}

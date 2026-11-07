namespace LeftoverScout.Core.Storage;

/// <summary>
/// Checks whether a folder can be used to store the application's own data.
/// </summary>
public interface IDataFolderProbe
{
    /// <summary>
    /// Creates the folder if it does not exist and verifies that files can be written into it.
    /// Never throws; returns false when the folder cannot be created or written to.
    /// </summary>
    /// <param name="folderPath">Absolute path of the candidate folder.</param>
    bool TryEnsureWritable(string folderPath);
}

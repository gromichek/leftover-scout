using LeftoverScout.Core.Shell;

namespace LeftoverScout.Windows.Shell;

/// <summary>
/// Checks folder existence with <see cref="Directory.Exists(string)"/>: a metadata check that does not
/// open or enumerate the folder.
/// </summary>
public sealed class FileSystemFolderExistence : IFolderExistence
{
    /// <inheritdoc />
    public bool FolderExists(string folderPath)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}

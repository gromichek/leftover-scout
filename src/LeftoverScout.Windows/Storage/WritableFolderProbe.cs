using LeftoverScout.Core.Storage;

namespace LeftoverScout.Windows.Storage;

/// <summary>
/// Checks that a folder is writable by creating and removing a uniquely named probe file in it.
/// </summary>
public sealed class WritableFolderProbe : IDataFolderProbe
{
    /// <inheritdoc />
    public bool TryEnsureWritable(string folderPath)
    {
        string? probePath = null;
        try
        {
            Directory.CreateDirectory(folderPath);

            probePath = Path.Combine(folderPath, $".write-test-{Guid.NewGuid():N}.tmp");
            using (new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return false;
        }
        finally
        {
            RemoveProbe(probePath);
        }
    }

    private static void RemoveProbe(string? probePath)
    {
        if (probePath is null)
        {
            return;
        }

        try
        {
            // The probe is the application's own file, created a moment ago. Nothing else is touched.
            File.Delete(probePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A leftover probe file is harmless.
        }
    }
}

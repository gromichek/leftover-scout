namespace LeftoverScout.Core.Cleanup;

/// <summary>Removes a whole folder, falling back to its children when something is in use (D-063).</summary>
public interface IFolderRemover
{
    /// <summary>Removes <paramref name="path"/> as far as possible.</summary>
    FolderRemoveResult Remove(string path, CleanupMode mode);
}

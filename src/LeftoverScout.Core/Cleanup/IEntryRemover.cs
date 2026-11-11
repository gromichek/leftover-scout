namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Removes one file or folder (with everything inside it) through the operating system. The Windows implementation
/// is the only code that deletes user data.
/// </summary>
public interface IEntryRemover
{
    /// <summary>Removes <paramref name="path"/>. Never throws for expected failures; they are returned.</summary>
    EntryRemoveResult Remove(string path, CleanupMode mode);
}

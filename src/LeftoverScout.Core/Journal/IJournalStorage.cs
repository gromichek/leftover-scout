namespace LeftoverScout.Core.Journal;

/// <summary>The small file system surface the journal needs. Implemented by <see cref="FileJournalStorage"/>.</summary>
public interface IJournalStorage
{
    /// <summary>Full path of the journal folder.</summary>
    string FolderPath { get; }

    /// <summary>Creates the journal folder when it does not exist. Returns false when that failed; never throws.</summary>
    bool EnsureFolder();

    /// <summary>
    /// Writes the file atomically: the text goes to a temporary file in the same folder, which then replaces the target.
    /// The journal folder is created when needed. Throws <see cref="IOException"/> or
    /// <see cref="UnauthorizedAccessException"/> on failure.
    /// </summary>
    void WriteAtomic(string fileName, string content);

    /// <summary>Names (not paths) of the journal files, in no particular order. Empty when the folder does not exist.</summary>
    IReadOnlyList<string> ListFileNames();

    /// <summary>Reads a journal file as text.</summary>
    string ReadAllText(string fileName);
}

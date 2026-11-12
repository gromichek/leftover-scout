namespace LeftoverScout.Core.Journal;

/// <summary>Reads the journals, read-only.</summary>
public interface IJournalReader
{
    /// <summary>
    /// Reads every journal file, newest first. A file that cannot be read is returned as an unreadable entry; this
    /// method never throws because of a bad file.
    /// </summary>
    IReadOnlyList<JournalEntry> ReadAll();
}

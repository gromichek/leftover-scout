namespace LeftoverScout.Core.Journal;

/// <summary>A cleanup session that is being recorded. Created by <see cref="ICleanupJournal.Begin"/>.</summary>
public sealed class JournalHandle
{
    internal JournalHandle(JournalDocument document, string fileName)
    {
        Document = document;
        FileName = fileName;
    }

    /// <summary>The identifier of the session.</summary>
    public Guid Id => Document.Id;

    /// <summary>
    /// A short English message when the journal could not be written completely (the cleanup itself went on);
    /// null when every write succeeded. Never contains paths.
    /// </summary>
    public string? Warning { get; internal set; }

    internal JournalDocument Document { get; }

    internal string FileName { get; }

    internal object Gate { get; } = new();
}

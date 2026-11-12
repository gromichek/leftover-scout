namespace LeftoverScout.Core.Journal;

/// <summary>Reads the journal files from <see cref="IJournalStorage"/>.</summary>
public sealed class JournalReader : IJournalReader
{
    private readonly IJournalStorage _storage;
    private readonly ActiveJournalSessions _active;

    /// <summary>Creates a reader.</summary>
    /// <param name="storage">Where the files are.</param>
    /// <param name="active">The sessions running in this process; other in-progress files are interrupted.</param>
    public JournalReader(IJournalStorage storage, ActiveJournalSessions active)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(active);
        _storage = storage;
        _active = active;
    }

    /// <inheritdoc />
    public IReadOnlyList<JournalEntry> ReadAll()
    {
        var entries = new List<JournalEntry>();
        foreach (string fileName in _storage.ListFileNames())
        {
            entries.Add(ReadOne(fileName));
        }

        return entries
            .OrderByDescending(entry => entry.SortTime)
            .ThenByDescending(entry => entry.FileName, StringComparer.Ordinal)
            .ToList();
    }

    private JournalEntry ReadOne(string fileName)
    {
        DateTime fromName = JournalFileName.TryParseTime(fileName) ?? DateTime.MinValue;
        JournalDocument? document = null;
        try
        {
            document = JournalJson.TryParse(_storage.ReadAllText(fileName));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Treated like any other unreadable file.
        }

        if (document is null)
        {
            return new JournalEntry(fileName, JournalEntryStatus.Unreadable, null, fromName);
        }

        JournalEntryStatus status = document.State switch
        {
            JournalState.Completed => JournalEntryStatus.Completed,
            JournalState.Cancelled => JournalEntryStatus.Cancelled,
            JournalState.Failed => JournalEntryStatus.Failed,
            _ => _active.IsActive(document.Id) ? JournalEntryStatus.InProgress : JournalEntryStatus.Interrupted,
        };

        return new JournalEntry(fileName, status, document, document.StartedAt.DateTime);
    }
}

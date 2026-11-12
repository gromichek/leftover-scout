using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Journal;
using static LeftoverScout.Core.Tests.Journal.JournalTestKit;

namespace LeftoverScout.Core.Tests.Journal;

public class JournalReaderTests
{
    private static JournalDocument Document(DateTimeOffset started, JournalState state) => new()
    {
        SchemaVersion = JournalDocument.CurrentSchemaVersion,
        Id = Guid.NewGuid(),
        AppVersion = "0.1.0",
        StartedAt = started,
        State = state,
        Mode = CleanupMode.RecycleBin,
        ProfileRoot = Root,
    };

    private static string Put(InMemoryJournalStorage storage, JournalDocument document)
    {
        string name = JournalFileName.Create(document.StartedAt, document.Id);
        storage.Files[name] = JournalJson.Serialize(document);
        return name;
    }

    [Fact]
    public void ReadAll_SortsNewestFirst()
    {
        var storage = new InMemoryJournalStorage();
        string older = Put(storage, Document(Now.AddDays(-2), JournalState.Completed));
        string newest = Put(storage, Document(Now, JournalState.Completed));
        string middle = Put(storage, Document(Now.AddDays(-1), JournalState.Cancelled));

        IReadOnlyList<JournalEntry> entries = new JournalReader(storage, new ActiveJournalSessions()).ReadAll();

        Assert.Equal([newest, middle, older], entries.Select(entry => entry.FileName));
    }

    [Fact]
    public void ReadAll_CorruptFile_IsUnreadableEntryWithItsName()
    {
        var storage = new InMemoryJournalStorage();
        storage.Files["cleanup-20261112-103000-deadbeef.json"] = "{ this is not json";
        string good = Put(storage, Document(Now.AddDays(-1), JournalState.Completed));

        IReadOnlyList<JournalEntry> entries = new JournalReader(storage, new ActiveJournalSessions()).ReadAll();

        Assert.Equal(2, entries.Count);
        JournalEntry bad = entries.Single(entry => entry.FileName == "cleanup-20261112-103000-deadbeef.json");
        Assert.Equal(JournalEntryStatus.Unreadable, bad.Status);
        Assert.Null(bad.Document);
        Assert.Equal(JournalEntryStatus.Completed, entries.Single(entry => entry.FileName == good).Status);
    }

    [Fact]
    public void ReadAll_UnknownSchemaVersion_IsUnreadable()
    {
        var storage = new InMemoryJournalStorage();
        JournalDocument document = Document(Now, JournalState.Completed);
        document.SchemaVersion = 2;
        string name = Put(storage, document);

        JournalEntry entry = Assert.Single(new JournalReader(storage, new ActiveJournalSessions()).ReadAll());

        Assert.Equal(name, entry.FileName);
        Assert.Equal(JournalEntryStatus.Unreadable, entry.Status);
    }

    [Fact]
    public void ReadAll_UnknownProperty_IsUnreadable()
    {
        var storage = new InMemoryJournalStorage();
        JournalDocument document = Document(Now, JournalState.Completed);
        string json = JournalJson.Serialize(document).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"surprise\": true", StringComparison.Ordinal);
        storage.Files[JournalFileName.Create(Now, document.Id)] = json;

        Assert.Equal(JournalEntryStatus.Unreadable, Assert.Single(new JournalReader(storage, new ActiveJournalSessions()).ReadAll()).Status);
    }

    [Fact]
    public void ReadAll_InProgressNotRunningHere_IsInterrupted()
    {
        var storage = new InMemoryJournalStorage();
        Put(storage, Document(Now, JournalState.InProgress));

        JournalEntry entry = Assert.Single(new JournalReader(storage, new ActiveJournalSessions()).ReadAll());

        Assert.Equal(JournalEntryStatus.Interrupted, entry.Status);
    }

    [Fact]
    public void ReadAll_InProgressRunningHere_IsInProgress()
    {
        var storage = new InMemoryJournalStorage();
        var active = new ActiveJournalSessions();
        JournalDocument document = Document(Now, JournalState.InProgress);
        Put(storage, document);
        active.Add(document.Id);

        Assert.Equal(JournalEntryStatus.InProgress, Assert.Single(new JournalReader(storage, active).ReadAll()).Status);
    }

    [Fact]
    public void ReadAll_FileThatCannotBeRead_IsUnreadable()
    {
        var storage = new InMemoryJournalStorage { FailReads = true };
        Put(storage, Document(Now, JournalState.Completed));

        JournalEntry entry = Assert.Single(new JournalReader(storage, new ActiveJournalSessions()).ReadAll());

        Assert.Equal(JournalEntryStatus.Unreadable, entry.Status);
    }

    [Fact]
    public void WrittenJournal_CanBeReadBack()
    {
        var storage = new InMemoryJournalStorage();
        var active = new ActiveJournalSessions();
        CleanupJournal journal = NewJournal(storage, active);
        CleanupItem a = Item("A");
        JournalHandle handle = journal.Begin(Request(a), CleanupMode.RecycleBin);
        journal.Complete(handle, new CleanupSessionResult(Now.UtcDateTime, Now.UtcDateTime, CleanupMode.RecycleBin, [Removed(a)]));

        JournalEntry entry = Assert.Single(new JournalReader(storage, active).ReadAll());

        Assert.Equal(JournalEntryStatus.Completed, entry.Status);
        Assert.Equal(1000, entry.Document!.Totals.BytesRemoved);
        Assert.Equal(JournalOutcome.Removed, entry.Document.Items[0].Outcome);
    }
}

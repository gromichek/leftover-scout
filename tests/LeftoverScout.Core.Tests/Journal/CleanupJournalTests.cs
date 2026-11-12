using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Journal;
using static LeftoverScout.Core.Tests.Journal.JournalTestKit;

namespace LeftoverScout.Core.Tests.Journal;

public class CleanupJournalTests
{
    [Fact]
    public void Begin_CreatesFileBeforeFirstItem_WithAllItemsPending()
    {
        var storage = new InMemoryJournalStorage();
        CleanupJournal journal = NewJournal(storage);

        JournalHandle handle = journal.Begin(Request(Item("A"), Item("B")), CleanupMode.RecycleBin);

        JournalDocument document = Parse(storage, handle);
        Assert.Equal(JournalState.InProgress, document.State);
        Assert.Equal(JournalDocument.CurrentSchemaVersion, document.SchemaVersion);
        Assert.Equal("0.1.0-test", document.AppVersion);
        Assert.Equal(CleanupMode.RecycleBin, document.Mode);
        Assert.Equal(Root, document.ProfileRoot);
        Assert.Null(document.EndedAt);
        Assert.All(document.Items, item => Assert.Equal(JournalOutcome.Pending, item.Outcome));
        Assert.Equal(@"AppData\Roaming\A", document.Items[0].RelativePath);
        Assert.Equal(2, document.Totals.ItemCount);
    }

    [Fact]
    public void Record_RewritesFileAfterEachItem()
    {
        var storage = new InMemoryJournalStorage();
        CleanupJournal journal = NewJournal(storage);
        CleanupItem a = Item("A");
        CleanupItem b = Item("B");
        JournalHandle handle = journal.Begin(Request(a, b), CleanupMode.RecycleBin);

        journal.Record(handle, Removed(a));
        JournalDocument afterFirst = Parse(storage, handle);
        journal.Record(handle, new CleanupItemResult(b, CleanupOutcome.Blocked, Reason: "Blocked by the guard."));
        JournalDocument afterSecond = Parse(storage, handle);

        Assert.Equal(JournalOutcome.Removed, afterFirst.Items[0].Outcome);
        Assert.Equal(JournalOutcome.Pending, afterFirst.Items[1].Outcome);
        Assert.Equal(1000, afterFirst.Totals.BytesRemoved);
        Assert.Equal(JournalOutcome.Blocked, afterSecond.Items[1].Outcome);
        Assert.Equal("Blocked by the guard.", afterSecond.Items[1].Reason);
        Assert.Single(storage.Files);
        Assert.Equal(JournalState.InProgress, afterSecond.State);
    }

    [Fact]
    public void Complete_FinalizesWithTotalsAndEndTime()
    {
        var storage = new InMemoryJournalStorage();
        var active = new ActiveJournalSessions();
        CleanupJournal journal = NewJournal(storage, active);
        CleanupItem a = Item("A");
        CleanupItem b = Item("B", 500);
        JournalHandle handle = journal.Begin(Request(a, b), CleanupMode.Permanent);
        var results = new List<CleanupItemResult>
        {
            Removed(a),
            new(b, CleanupOutcome.PartlyRemoved, 200, 3, "In use.", DeletedPermanentlyUnexpectedly: true),
        };

        journal.Complete(handle, new CleanupSessionResult(Now.UtcDateTime, Now.UtcDateTime, CleanupMode.Permanent, results));

        JournalDocument document = Parse(storage, handle);
        Assert.Equal(JournalState.Completed, document.State);
        Assert.NotNull(document.EndedAt);
        Assert.Equal(1200, document.Totals.BytesRemoved);
        Assert.Equal(1, document.Totals.Outcomes["Removed"]);
        Assert.Equal(1, document.Totals.Outcomes["PartlyRemoved"]);
        Assert.Equal(3, document.Items[1].ItemsLeft);
        Assert.True(document.Items[1].DeletedPermanentlyUnexpectedly);
        Assert.False(active.IsActive(handle.Id));
    }

    [Fact]
    public void Complete_WithNotStartedItems_IsCancelled()
    {
        var storage = new InMemoryJournalStorage();
        CleanupJournal journal = NewJournal(storage);
        CleanupItem a = Item("A");
        CleanupItem b = Item("B");
        JournalHandle handle = journal.Begin(Request(a, b), CleanupMode.RecycleBin);
        var results = new List<CleanupItemResult> { Removed(a), new(b, CleanupOutcome.NotStarted, Reason: "Cancelled.") };

        journal.Complete(handle, new CleanupSessionResult(Now.UtcDateTime, Now.UtcDateTime, CleanupMode.RecycleBin, results));

        Assert.Equal(JournalState.Cancelled, Parse(storage, handle).State);
    }

    [Fact]
    public void Abort_Failed_MarksPendingItemsNotStarted()
    {
        var storage = new InMemoryJournalStorage();
        CleanupJournal journal = NewJournal(storage);
        CleanupItem a = Item("A");
        JournalHandle handle = journal.Begin(Request(a, Item("B")), CleanupMode.RecycleBin);
        journal.Record(handle, Removed(a));

        journal.Abort(handle, JournalState.Failed);

        JournalDocument document = Parse(storage, handle);
        Assert.Equal(JournalState.Failed, document.State);
        Assert.Equal(JournalOutcome.Removed, document.Items[0].Outcome);
        Assert.Equal(JournalOutcome.NotStarted, document.Items[1].Outcome);
    }

    [Fact]
    public void Abort_WithCompletedState_Throws()
    {
        CleanupJournal journal = NewJournal(new InMemoryJournalStorage());
        JournalHandle handle = journal.Begin(Request(Item("A")), CleanupMode.RecycleBin);

        Assert.Throws<ArgumentException>(() => journal.Abort(handle, JournalState.Completed));
    }

    [Fact]
    public void Begin_NestedAndDuplicateItems_AreReducedLikeTheCleanupService()
    {
        var storage = new InMemoryJournalStorage();
        CleanupJournal journal = NewJournal(storage);
        CleanupItem parent = Item("A");
        CleanupItem child = new(parent.FullPath + @"\Cache", parent.Kind, parent.Confidence, 10, null);

        JournalHandle handle = journal.Begin(Request(parent, child, parent), CleanupMode.RecycleBin);

        Assert.Single(Parse(storage, handle).Items);
    }

    [Fact]
    public void WriteFailure_SetsWarning_AndNeverThrows()
    {
        var storage = new InMemoryJournalStorage { FailWrites = true };
        CleanupJournal journal = NewJournal(storage);
        CleanupItem a = Item("A");

        JournalHandle handle = journal.Begin(Request(a), CleanupMode.RecycleBin);
        journal.Record(handle, Removed(a));
        journal.Complete(handle, new CleanupSessionResult(Now.UtcDateTime, Now.UtcDateTime, CleanupMode.RecycleBin, [Removed(a)]));

        Assert.NotNull(handle.Warning);
        Assert.DoesNotContain("SampleUser", handle.Warning, StringComparison.Ordinal);
    }

    [Fact]
    public void SuccessfulWrites_LeaveNoWarning()
    {
        CleanupJournal journal = NewJournal(new InMemoryJournalStorage());

        JournalHandle handle = journal.Begin(Request(Item("A")), CleanupMode.RecycleBin);

        Assert.Null(handle.Warning);
    }

    [Fact]
    public void BeginAndComplete_RaiseSessionRecorded()
    {
        CleanupJournal journal = NewJournal(new InMemoryJournalStorage());
        int raised = 0;
        journal.SessionRecorded += (_, _) => raised++;
        CleanupItem a = Item("A");

        JournalHandle handle = journal.Begin(Request(a), CleanupMode.RecycleBin);
        journal.Complete(handle, new CleanupSessionResult(Now.UtcDateTime, Now.UtcDateTime, CleanupMode.RecycleBin, [Removed(a)]));

        Assert.Equal(2, raised);
    }

    [Fact]
    public void Begin_FileNameFollowsFormat()
    {
        var storage = new InMemoryJournalStorage();
        CleanupJournal journal = NewJournal(storage);

        JournalHandle handle = journal.Begin(Request(Item("A")), CleanupMode.RecycleBin);

        string name = Assert.Single(storage.Files.Keys);
        Assert.Matches(@"^cleanup-\d{8}-\d{6}-[0-9a-f]{8}\.json$", name);
        Assert.Contains(handle.Id.ToString("N")[..8], name, StringComparison.Ordinal);
    }
}

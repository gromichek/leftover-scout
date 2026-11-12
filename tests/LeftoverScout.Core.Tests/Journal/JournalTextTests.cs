using LeftoverScout.Core.Journal;
using static LeftoverScout.Core.Tests.Journal.JournalTestKit;

namespace LeftoverScout.Core.Tests.Journal;

public class JournalTextTests
{
    private static JournalEntry Entry(JournalEntryStatus status, params JournalOutcome[] outcomes)
    {
        var document = new JournalDocument
        {
            SchemaVersion = 1,
            StartedAt = Now,
            Items = [.. outcomes.Select(outcome => new JournalItem { Outcome = outcome })],
        };
        return new JournalEntry("cleanup-20261112-103000-abcd1234.json", status, document, Now.DateTime);
    }

    [Fact]
    public void Result_Completed_ListsNonZeroCounts()
    {
        JournalEntry entry = Entry(
            JournalEntryStatus.Completed,
            [.. Enumerable.Repeat(JournalOutcome.Removed, 12), JournalOutcome.PartlyRemoved, JournalOutcome.SkippedRecycleBinOff, JournalOutcome.NotRecycled]);

        Assert.Equal("12 removed · 1 partly · 2 skipped", JournalText.Result(entry));
    }

    [Fact]
    public void Result_StatesWithoutCounts()
    {
        Assert.Equal("Interrupted", JournalText.Result(Entry(JournalEntryStatus.Interrupted, JournalOutcome.Removed)));
        Assert.Equal("In progress", JournalText.Result(Entry(JournalEntryStatus.InProgress)));
        Assert.Equal("Unreadable journal", JournalText.Result(new JournalEntry("x.json", JournalEntryStatus.Unreadable, null, DateTime.MinValue)));
    }

    [Fact]
    public void Result_CancelledAndFailed_ArePrefixed()
    {
        Assert.Equal("Cancelled · 1 removed · 2 not started", JournalText.Result(Entry(JournalEntryStatus.Cancelled, JournalOutcome.Removed, JournalOutcome.NotStarted, JournalOutcome.NotStarted)));
        Assert.Equal("Failed", JournalText.Result(Entry(JournalEntryStatus.Failed)));
    }

    [Fact]
    public void Result_NoItems_SaysNothingToDo()
    {
        Assert.Equal("Nothing to do", JournalText.Result(Entry(JournalEntryStatus.Completed)));
    }

    [Fact]
    public void Outcome_EveryValueHasAText()
    {
        foreach (JournalOutcome outcome in Enum.GetValues<JournalOutcome>())
        {
            Assert.False(string.IsNullOrWhiteSpace(JournalText.Outcome(outcome)));
        }

        Assert.Equal("Partly removed", JournalText.Outcome(JournalOutcome.PartlyRemoved));
    }
}

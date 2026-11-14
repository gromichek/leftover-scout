using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Formatting;

namespace LeftoverScout.Core.Tests.Cleanup;

public class CleanupTextTests
{
    private static CleanupItem Item() =>
        new(@"C:\Users\SampleUser\AppData\Roaming\OldApp", FindingKind.Leftover, LeftoverConfidence.High, 2048, null);

    private static CleanupSessionResult Session(CleanupMode mode, params CleanupItemResult[] results) =>
        new(DateTime.UnixEpoch, DateTime.UnixEpoch, mode, results);

    [Fact]
    public void Items_UsesSingularForOne()
    {
        Assert.Equal("1 item", CleanupText.Items(1));
        Assert.Equal("2 items", CleanupText.Items(2));
        Assert.Equal("0 items", CleanupText.Items(0));
    }

    [Fact]
    public void SelectionLine_ShowsCountAndSize()
    {
        Assert.Equal($"Selected: 2 items · {SizeFormatter.Format(2048)}", CleanupText.SelectionLine(2, 2048));
    }

    [Fact]
    public void MergedLine_DescribesNestedItems()
    {
        Assert.Equal(string.Empty, CleanupText.MergedLine(0));
        Assert.Equal("1 item is inside another selected item and is included with it.", CleanupText.MergedLine(1));
        Assert.Equal("2 items are inside other selected items and are included with them.", CleanupText.MergedLine(2));
    }

    [Fact]
    public void FirstQuestion_RecycleBin_AsksToMove()
    {
        string text = CleanupText.FirstQuestion(CleanupMode.RecycleBin, 3, 2048, 0);

        Assert.Equal($"Move 3 items ({SizeFormatter.Format(2048)}) to the Recycle Bin?", text);
    }

    [Fact]
    public void FirstQuestion_Permanent_SaysPermanently()
    {
        string text = CleanupText.FirstQuestion(CleanupMode.Permanent, 1, 2048, 0);

        Assert.Equal($"Delete 1 item ({SizeFormatter.Format(2048)}) permanently?", text);
    }

    [Fact]
    public void FirstQuestion_LowConfidenceItems_AreMentioned()
    {
        string text = CleanupText.FirstQuestion(CleanupMode.RecycleBin, 3, 2048, 2);

        Assert.Contains("Including 2 items with Low confidence", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SecondQuestion_SaysItCannotBeUndone()
    {
        Assert.Equal(
            $"This cannot be undone. Delete 3 items ({SizeFormatter.Format(2048)}) permanently?",
            CleanupText.SecondQuestion(3, 2048));
    }

    [Theory]
    [InlineData(CleanupOutcome.Removed, "Removed")]
    [InlineData(CleanupOutcome.PartlyRemoved, "Partly removed")]
    [InlineData(CleanupOutcome.SkippedTooLargeForRecycleBin, "Skipped (too large)")]
    [InlineData(CleanupOutcome.SkippedRecycleBinOff, "Skipped (bin off)")]
    [InlineData(CleanupOutcome.NotRecycled, "Not recycled")]
    [InlineData(CleanupOutcome.Blocked, "Blocked")]
    [InlineData(CleanupOutcome.NotFound, "Not found")]
    [InlineData(CleanupOutcome.Failed, "Failed")]
    [InlineData(CleanupOutcome.NotStarted, "Not started")]
    public void Outcome_HasPlainWords(CleanupOutcome outcome, string expected)
    {
        Assert.Equal(expected, CleanupText.Outcome(outcome));
    }

    [Fact]
    public void SummaryLines_EveryOutcomeHasItsOwnSentence()
    {
        CleanupItem item = Item();
        CleanupSessionResult session = Session(
            CleanupMode.RecycleBin,
            new CleanupItemResult(item, CleanupOutcome.Removed, 100),
            new CleanupItemResult(item, CleanupOutcome.PartlyRemoved, 50, 2, "Some files are in use."),
            new CleanupItemResult(item, CleanupOutcome.SkippedTooLargeForRecycleBin, Reason: "Too large."),
            new CleanupItemResult(item, CleanupOutcome.SkippedRecycleBinOff, Reason: "Bin off."),
            new CleanupItemResult(item, CleanupOutcome.NotRecycled, Reason: "Vetoed."),
            new CleanupItemResult(item, CleanupOutcome.Blocked, BlockReasons: [BlockReason.Of(BlockReasonCode.NotFound)]),
            new CleanupItemResult(item, CleanupOutcome.NotFound),
            new CleanupItemResult(item, CleanupOutcome.Failed, Reason: "Access denied."),
            new CleanupItemResult(item, CleanupOutcome.NotStarted, Reason: "Cancelled."));

        IReadOnlyList<string> lines = CleanupText.SummaryLines(session);

        Assert.Equal(9, lines.Count);
        Assert.Equal("1 item moved to the Recycle Bin.", lines[0]);
        Assert.Contains("partly", lines[1], StringComparison.Ordinal);
        Assert.Contains("too large", lines[2], StringComparison.Ordinal);
        Assert.Contains("turned off", lines[3], StringComparison.Ordinal);
        Assert.Contains("not moved", lines[4], StringComparison.Ordinal);
        Assert.Contains("blocked", lines[5], StringComparison.Ordinal);
        Assert.Contains("not found", lines[6], StringComparison.Ordinal);
        Assert.Contains("could not be removed", lines[7], StringComparison.Ordinal);
        Assert.Contains("cancelled", lines[8], StringComparison.Ordinal);
    }

    [Fact]
    public void SummaryLines_PermanentMode_SaysDeletedPermanently()
    {
        CleanupSessionResult session = Session(CleanupMode.Permanent, new CleanupItemResult(Item(), CleanupOutcome.Removed, 100));

        Assert.Equal(["1 item deleted permanently."], CleanupText.SummaryLines(session));
    }

    [Fact]
    public void Reason_CombinesBlockReasonsEntriesLeftAndUnexpectedPermanent()
    {
        CleanupItem item = Item();

        Assert.Equal(string.Empty, CleanupText.Reason(new CleanupItemResult(item, CleanupOutcome.Removed, 10)));
        Assert.Contains(
            "does not exist",
            CleanupText.Reason(new CleanupItemResult(item, CleanupOutcome.Blocked, BlockReasons: [BlockReason.Of(BlockReasonCode.NotFound)])),
            StringComparison.Ordinal);

        string partly = CleanupText.Reason(new CleanupItemResult(item, CleanupOutcome.PartlyRemoved, 5, 3, "Some files are in use."));
        Assert.Contains("Some files are in use.", partly, StringComparison.Ordinal);
        Assert.Contains("3 entries left", partly, StringComparison.Ordinal);

        string unexpected = CleanupText.Reason(new CleanupItemResult(item, CleanupOutcome.Removed, 5, DeletedPermanentlyUnexpectedly: true));
        Assert.Contains("not moved to the Recycle Bin", unexpected, StringComparison.Ordinal);
    }
}

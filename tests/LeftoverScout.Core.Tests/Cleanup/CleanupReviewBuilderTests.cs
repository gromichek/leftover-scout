using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Cleanup;

public class CleanupReviewBuilderTests
{
    private const string Root = @"C:\Users\SampleUser";
    private static readonly ScanTarget Target = new(Root, isCurrentUser: true);

    private static CleanupItem Item(string name, LeftoverConfidence? confidence = LeftoverConfidence.High, long size = 1000, string? program = null) =>
        new(Root + @"\AppData\Roaming\" + name, confidence is null ? FindingKind.Cache : FindingKind.Leftover, confidence, size, null, program);

    private static GuardItemResult Allowed(CleanupItem item, params GuardWarningCode[] warnings) =>
        new(item, new GuardResult([], [.. warnings.Select(GuardWarning.Of)]));

    private static GuardItemResult Blocked(CleanupItem item, BlockReasonCode code) =>
        new(item, new GuardResult([BlockReason.Of(code)], []));

    private static CleanupReview Build(CleanupGuardReport report, params RunningProgramWarning[] running) =>
        CleanupReviewBuilder.Build(new CleanupRequest(Target, [.. report.Items.Select(entry => entry.Item)]), report, running);

    [Fact]
    public void Build_AllowedWithoutWarnings_IsReady()
    {
        CleanupItem item = Item("OldApp");

        CleanupReview review = Build(new CleanupGuardReport(new RequestReduction([item], []), [Allowed(item)]));

        CleanupReviewRow row = Assert.Single(review.Rows);
        Assert.True(row.IsReady);
        Assert.Equal("Ready", row.StatusText);
        Assert.Equal(@"AppData\Roaming\OldApp", row.RelativePath);
        Assert.Equal("Leftover", row.KindText);
        Assert.Equal("High", row.ConfidenceText);
        Assert.Equal(1, review.ReadyCount);
        Assert.Equal(1000, review.ReadySize);
    }

    [Fact]
    public void Build_BlockedItem_IsExcludedAndShowsTheReason()
    {
        CleanupItem ok = Item("OldApp");
        CleanupItem blocked = Item("Gone", size: 500);

        CleanupReview review = Build(new CleanupGuardReport(
            new RequestReduction([ok, blocked], []),
            [Allowed(ok), Blocked(blocked, BlockReasonCode.NotFound)]));

        CleanupReviewRow row = review.Rows.Single(r => !r.IsReady);
        Assert.StartsWith("Blocked: ", row.StatusText, StringComparison.Ordinal);
        Assert.Contains("does not exist", row.StatusText, StringComparison.Ordinal);
        Assert.Equal([ok], review.ReadyItems);
        Assert.Equal(1, review.BlockedCount);
        Assert.Equal(1000, review.ReadySize);
    }

    [Fact]
    public void Build_Warnings_AreShownInTheStatus()
    {
        CleanupItem low = Item("Fresh", LeftoverConfidence.Low);
        CleanupItem medium = Item("Maybe", LeftoverConfidence.Medium);
        CleanupItem changed = Item("Busy");

        CleanupReview review = Build(new CleanupGuardReport(
            new RequestReduction([low, medium, changed], []),
            [
                Allowed(low, GuardWarningCode.LowConfidence),
                Allowed(medium, GuardWarningCode.MediumConfidence),
                Allowed(changed, GuardWarningCode.ChangedSinceScan),
            ]));

        Assert.Equal("Ready · Low confidence", review.Rows[0].StatusText);
        Assert.Equal("Ready · Medium confidence", review.Rows[1].StatusText);
        Assert.Equal("Ready · Changed since the scan", review.Rows[2].StatusText);
        Assert.All(review.Rows, row => Assert.True(row.IsReady));
        Assert.Equal(1, review.ReadyLowConfidenceCount);
    }

    [Fact]
    public void Build_MergedItems_AreCounted()
    {
        CleanupItem outer = Item("Outer");
        CleanupItem inner = Item(@"Outer\Cache", null, 100);
        var reduction = new RequestReduction([outer], [new MergedItem(inner, outer.FullPath, MergeReason.InsideSelectedFolder)]);

        CleanupReview review = CleanupReviewBuilder.Build(
            new CleanupRequest(Target, [outer, inner]),
            new CleanupGuardReport(reduction, [Allowed(outer)]),
            []);

        Assert.Equal(1, review.MergedCount);
    }

    [Fact]
    public void Build_RunningPrograms_AreDistinctSortedAndOnlyForReadyItems()
    {
        CleanupItem ready = Item("Chat");
        CleanupItem blocked = Item("Other");
        var report = new CleanupGuardReport(
            new RequestReduction([ready, blocked], []),
            [Allowed(ready), Blocked(blocked, BlockReasonCode.NotFound)]);

        CleanupReview review = Build(
            report,
            new RunningProgramWarning(ready, ["Discord", "chat"]),
            new RunningProgramWarning(blocked, ["Skype"]),
            new RunningProgramWarning(ready, ["discord"]));

        Assert.Equal(["chat", "Discord"], review.RunningPrograms);
    }

    [Fact]
    public void Build_NothingReady_HasZeroTotals()
    {
        CleanupItem blocked = Item("Gone");

        CleanupReview review = Build(new CleanupGuardReport(new RequestReduction([blocked], []), [Blocked(blocked, BlockReasonCode.NotFound)]));

        Assert.Equal(0, review.ReadyCount);
        Assert.Equal(0, review.ReadySize);
        Assert.Empty(review.ReadyItems);
    }
}

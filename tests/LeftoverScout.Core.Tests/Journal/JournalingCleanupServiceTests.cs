using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Journal;
using static LeftoverScout.Core.Tests.Journal.JournalTestKit;

namespace LeftoverScout.Core.Tests.Journal;

public class JournalingCleanupServiceTests
{
    private sealed class FakeInnerService : ICleanupService
    {
        public Func<CleanupRequest, Action<CleanupItemResult>?, CleanupSessionResult>? Behavior { get; set; }

        public Task<CleanupSessionResult> RunAsync(
            CleanupRequest request,
            CleanupMode mode,
            IProgress<CleanupProgress>? progress,
            CancellationToken cancellationToken,
            Action<CleanupItemResult>? itemCompleted = null) =>
            Task.FromResult(Behavior!(request, itemCompleted));
    }

    private static CleanupSessionResult Session(CleanupMode mode, params CleanupItemResult[] results) =>
        new(Now.UtcDateTime, Now.UtcDateTime, mode, results);

    [Fact]
    public async Task RunAsync_Success_WritesBeforeDuringAndAfter()
    {
        var storage = new InMemoryJournalStorage();
        CleanupItem a = Item("A");
        CleanupItem b = Item("B");
        var seenWhileRunning = new List<JournalOutcome[]>();
        var inner = new FakeInnerService();
        inner.Behavior = (request, callback) =>
        {
            seenWhileRunning.Add(Snapshot(storage));
            callback!(Removed(a));
            seenWhileRunning.Add(Snapshot(storage));
            callback(Removed(b));
            return Session(CleanupMode.RecycleBin, Removed(a), Removed(b));
        };
        var service = new JournalingCleanupService(inner, NewJournal(storage));

        CleanupSessionResult result = await service.RunAsync(Request(a, b), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken);

        Assert.Equal(new[] { JournalOutcome.Pending, JournalOutcome.Pending }, seenWhileRunning[0]);
        Assert.Equal(new[] { JournalOutcome.Removed, JournalOutcome.Pending }, seenWhileRunning[1]);
        JournalDocument final = JournalJson.TryParse(Assert.Single(storage.Files).Value)!;
        Assert.Equal(JournalState.Completed, final.State);
        Assert.All(final.Items, item => Assert.Equal(JournalOutcome.Removed, item.Outcome));
        Assert.Null(result.JournalWarning);
        Assert.Equal(2000, result.BytesRemoved);
    }

    [Fact]
    public async Task RunAsync_Cancelled_RecordsNotStartedItems()
    {
        var storage = new InMemoryJournalStorage();
        CleanupItem a = Item("A");
        CleanupItem b = Item("B");
        var inner = new FakeInnerService
        {
            Behavior = (_, callback) =>
            {
                var notStarted = new CleanupItemResult(b, CleanupOutcome.NotStarted, Reason: "Cancelled.");
                callback!(Removed(a));
                callback(notStarted);
                return Session(CleanupMode.RecycleBin, Removed(a), notStarted);
            },
        };
        var service = new JournalingCleanupService(inner, NewJournal(storage));

        await service.RunAsync(Request(a, b), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken);

        JournalDocument final = JournalJson.TryParse(Assert.Single(storage.Files).Value)!;
        Assert.Equal(JournalState.Cancelled, final.State);
        Assert.Equal(JournalOutcome.NotStarted, final.Items[1].Outcome);
    }

    [Fact]
    public async Task RunAsync_Exception_IsRethrown_AndJournalIsFailed()
    {
        var storage = new InMemoryJournalStorage();
        CleanupItem a = Item("A");
        CleanupItem b = Item("B");
        var inner = new FakeInnerService
        {
            Behavior = (_, callback) =>
            {
                callback!(Removed(a));
                throw new InvalidOperationException("Boom.");
            },
        };
        var active = new ActiveJournalSessions();
        var service = new JournalingCleanupService(inner, NewJournal(storage, active));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RunAsync(Request(a, b), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken));

        JournalDocument final = JournalJson.TryParse(Assert.Single(storage.Files).Value)!;
        Assert.Equal(JournalState.Failed, final.State);
        Assert.Equal(JournalOutcome.Removed, final.Items[0].Outcome);
        Assert.Equal(JournalOutcome.NotStarted, final.Items[1].Outcome);
        Assert.Empty(new JournalReader(storage, active).ReadAll().Where(entry => entry.Status == JournalEntryStatus.Interrupted));
    }

    [Fact]
    public async Task RunAsync_JournalWriteFails_CleanupStillSucceeds_WithWarning()
    {
        var storage = new InMemoryJournalStorage { FailWrites = true };
        CleanupItem a = Item("A");
        var inner = new FakeInnerService
        {
            Behavior = (_, callback) =>
            {
                callback!(Removed(a));
                return Session(CleanupMode.RecycleBin, Removed(a));
            },
        };
        var service = new JournalingCleanupService(inner, NewJournal(storage));

        CleanupSessionResult result = await service.RunAsync(Request(a), CleanupMode.RecycleBin, null, TestContext.Current.CancellationToken);

        Assert.Equal(CleanupOutcome.Removed, result.Items[0].Outcome);
        Assert.NotNull(result.JournalWarning);
    }

    private static JournalOutcome[] Snapshot(InMemoryJournalStorage storage) =>
        [.. JournalJson.TryParse(Assert.Single(storage.Files).Value)!.Items.Select(item => item.Outcome)];
}

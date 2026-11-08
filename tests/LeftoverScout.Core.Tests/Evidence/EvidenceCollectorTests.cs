using LeftoverScout.Core.Evidence;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Core.Tests.Evidence;

public class EvidenceCollectorTests
{
    private sealed class FakeSource(ProgramSource source, Func<CancellationToken, IReadOnlyList<InstalledProgram>> collect) : IInstalledProgramSource
    {
        public ProgramSource Source => source;

        public IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken) => collect(cancellationToken);
    }

    private static InstalledProgram Program(string name, ProgramSource source) => new(name, source, name);

    private static EvidenceCollector Create(params IInstalledProgramSource[] sources) =>
        new(sources, NullLogger<EvidenceCollector>.Instance, TimeProvider.System);

    [Fact]
    public async Task CollectAsync_AllSourcesWork_ReturnsAllProgramsAndCounts()
    {
        EvidenceCollector collector = Create(
            new FakeSource(ProgramSource.Registry, _ => [Program("A", ProgramSource.Registry), Program("B", ProgramSource.Registry)]),
            new FakeSource(ProgramSource.Store, _ => [Program("C", ProgramSource.Store)]));

        EvidenceSnapshot snapshot = await collector.CollectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["A", "B", "C"], snapshot.Programs.Select(p => p.Name).Order());
        Assert.Equal(2, snapshot.SourceCounts[ProgramSource.Registry]);
        Assert.Equal(1, snapshot.SourceCounts[ProgramSource.Store]);
        Assert.Empty(snapshot.Errors);
    }

    [Fact]
    public async Task CollectAsync_SourcesRunInParallel()
    {
        // Each source waits until the other one has started; this only completes when both run at the same time.
        using var firstStarted = new ManualResetEventSlim();
        using var secondStarted = new ManualResetEventSlim();
        EvidenceCollector collector = Create(
            new FakeSource(ProgramSource.Registry, _ =>
            {
                firstStarted.Set();
                Assert.True(WaitFor(secondStarted));
                return [];
            }),
            new FakeSource(ProgramSource.Store, _ =>
            {
                secondStarted.Set();
                Assert.True(WaitFor(firstStarted));
                return [];
            }));

        EvidenceSnapshot snapshot = await collector.CollectAsync(TestContext.Current.CancellationToken);

        Assert.Empty(snapshot.Errors);
    }

    [Fact]
    public async Task CollectAsync_OneSourceFails_OthersStillReturnAndErrorHasNoDetails()
    {
        EvidenceCollector collector = Create(
            new FakeSource(ProgramSource.Registry, _ => throw new InvalidOperationException(@"secret C:\Users\SampleUser\path")),
            new FakeSource(ProgramSource.Store, _ => [Program("C", ProgramSource.Store)]));

        EvidenceSnapshot snapshot = await collector.CollectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["C"], snapshot.Programs.Select(p => p.Name));
        Assert.Equal(0, snapshot.SourceCounts[ProgramSource.Registry]);
        EvidenceError error = Assert.Single(snapshot.Errors);
        Assert.Equal(ProgramSource.Registry, error.Source);
        Assert.Equal("Registry entries could not be read.", error.Message);
        Assert.DoesNotContain("SampleUser", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ProgramSource.Store, "Store apps could not be read.")]
    [InlineData(ProgramSource.ProgramFolder, "Program folders could not be read.")]
    [InlineData(ProgramSource.StartMenu, "Start menu could not be read.")]
    public async Task CollectAsync_FailingSource_HasShortMessage(ProgramSource source, string message)
    {
        EvidenceCollector collector = Create(new FakeSource(source, _ => throw new IOException("sample")));

        EvidenceSnapshot snapshot = await collector.CollectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(message, Assert.Single(snapshot.Errors).Message);
    }

    [Fact]
    public async Task CollectAsync_AlreadyCancelled_Throws()
    {
        EvidenceCollector collector = Create(new FakeSource(ProgramSource.Registry, _ => []));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => collector.CollectAsync(cts.Token));
    }

    [Fact]
    public async Task CollectAsync_CancelledWhileRunning_Throws()
    {
        using var started = new ManualResetEventSlim();
        using var cts = new CancellationTokenSource();
        EvidenceCollector collector = Create(new FakeSource(ProgramSource.Registry, token =>
        {
            started.Set();
            token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10));
            token.ThrowIfCancellationRequested();
            return [];
        }));

        Task<EvidenceSnapshot> running = collector.CollectAsync(cts.Token);
        Assert.True(WaitFor(started));
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
    }

    [Fact]
    public async Task CollectAsync_SetsCollectedAtFromClock()
    {
        var clock = new FixedClock(new DateTimeOffset(2025, 3, 4, 12, 30, 0, TimeSpan.Zero));
        var collector = new EvidenceCollector([], NullLogger<EvidenceCollector>.Instance, clock);

        EvidenceSnapshot snapshot = await collector.CollectAsync(TestContext.Current.CancellationToken);

        Assert.Equal(clock.GetUtcNow(), snapshot.CollectedAt);
        Assert.Empty(snapshot.Programs);
    }

    // A helper outside the test methods: the waits use a plain time-out because they run on pool threads.
    private static bool WaitFor(ManualResetEventSlim signal) => signal.Wait(TimeSpan.FromSeconds(10));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

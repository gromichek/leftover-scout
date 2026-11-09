using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using Microsoft.Extensions.Logging.Abstractions;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Scanning;

public class ScanServiceTests
{
    private sealed class NoPersonalFolders : IPersonalFolderProvider
    {
        public IReadOnlyList<string> GetPersonalFolderPaths() => [];
    }

    private sealed class FakeProgramSource(Func<CancellationToken, IReadOnlyList<InstalledProgram>> collect) : IInstalledProgramSource
    {
        public ProgramSource Source => ProgramSource.Registry;

        public IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken) => collect(cancellationToken);
    }

    private static readonly InstalledProgram SampleProgram = new("Sample App", ProgramSource.Registry, "sample key");

    private static ScanService Create(FakeDirectoryReader reader, Func<CancellationToken, IReadOnlyList<InstalledProgram>>? collect = null, RuleSet? ruleSet = null)
    {
        var collector = new EvidenceCollector(
            [new FakeProgramSource(collect ?? (_ => [SampleProgram]))],
            NullLogger<EvidenceCollector>.Instance,
            TimeProvider.System);
        return new ScanService(new ScanTarget(Root, true), reader, new NoPersonalFolders(), collector, new RuleMatcher(ruleSet ?? RuleSet.Empty), NullLogger<ScanService>.Instance, TimeProvider.System);
    }

    [Fact]
    public async Task RunAsync_Completed_StoresEvidenceWithTheResult()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));

        ScanResult result = await service.RunAsync(null, TestContext.Current.CancellationToken);

        Assert.NotNull(result.Evidence);
        Assert.Equal([SampleProgram], result.Evidence.Programs);
        Assert.Same(result.Evidence, service.LastResult!.Evidence);
    }

    [Fact]
    public async Task RunAsync_Completed_MatchesTheRulesOnceAndStoresThemWithTheResult()
    {
        RuleLoadResult rules = Rules.RuleTestHelper.Load(Rules.RuleTestHelper.Protected("protect-sub", @"{Profile}\Sub", "folder"));
        ScanService service = Create(
            new FakeDirectoryReader().Folder(Root, DirEntry("Sub"), FileEntry("a.txt", 3)),
            ruleSet: rules.RuleSet);

        ScanResult result = await service.RunAsync(null, TestContext.Current.CancellationToken);

        Assert.NotNull(result.RuleMatches);
        Assert.True(result.RuleMatches.Get(result.Root!.Children.Single())!.IsProtected);
        Assert.Same(result.RuleMatches, service.LastResult!.RuleMatches);
    }

    [Fact]
    public async Task RunAsync_Cancelled_KeepsPreviousRuleMatches()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));
        ScanResult first = await service.RunAsync(null, TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await service.RunAsync(null, cts.Token);

        Assert.Same(first.RuleMatches, service.LastResult!.RuleMatches);
    }

    [Fact]
    public async Task RunAsync_EvidenceSourceFails_ScanStillCompletesWithErrorRecorded()
    {
        ScanService service = Create(
            new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)),
            _ => throw new InvalidOperationException("sample failure"));

        ScanResult result = await service.RunAsync(null, TestContext.Current.CancellationToken);

        Assert.True(result.IsCompleted);
        Assert.Empty(result.Evidence!.Programs);
        Assert.Single(result.Evidence.Errors);
    }

    [Fact]
    public async Task RunAsync_Cancelled_KeepsPreviousResultAndItsEvidence()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));
        ScanResult first = await service.RunAsync(null, TestContext.Current.CancellationToken);
        EvidenceSnapshot? firstEvidence = first.Evidence;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        ScanResult second = await service.RunAsync(null, cts.Token);

        Assert.Equal(ScanCompletion.Cancelled, second.Completion);
        Assert.Null(second.Evidence);
        Assert.Same(first, service.LastResult);
        Assert.Same(firstEvidence, service.LastResult!.Evidence);
    }

    [Fact]
    public async Task RunAsync_EvidenceFinishesAfterScan_ReportsCollectingPrograms()
    {
        using var release = new ManualResetEventSlim();
        ScanService service = Create(
            new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)),
            _ =>
            {
                WaitFor(release);
                return [SampleProgram];
            });
        var seen = new List<string>();
        var progress = new SyncProgress(report =>
        {
            seen.Add(report.CurrentFolder);
            if (report.CurrentFolder == ScanService.CollectingProgramsText)
            {
                release.Set();
            }
        });

        ScanResult result = await service.RunAsync(progress, TestContext.Current.CancellationToken);

        Assert.Contains(ScanService.CollectingProgramsText, seen);
        Assert.Equal([SampleProgram], result.Evidence!.Programs);
    }

    private static bool WaitFor(ManualResetEventSlim signal) => signal.Wait(TimeSpan.FromSeconds(10));

    private sealed class SyncProgress(Action<ScanProgress> report) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value) => report(value);
    }

    [Fact]
    public async Task RunAsync_Completed_StoresLastResult()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));
        Assert.Null(service.LastResult);

        ScanResult result = await service.RunAsync(null, TestContext.Current.CancellationToken);

        Assert.Same(result, service.LastResult);
        Assert.Equal(3, result.Root!.LocalSize);
    }

    [Fact]
    public async Task RunAsync_Completed_RaisesLastResultChangedWithNewResultAndStampsTime()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));
        ScanResult? seenInEvent = null;
        service.LastResultChanged += (_, _) => seenInEvent = service.LastResult;

        ScanResult result = await service.RunAsync(null, TestContext.Current.CancellationToken);

        Assert.Same(result, seenInEvent);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task RunAsync_Cancelled_RaisesLastResultChangedButKeepsPreviousResult()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));
        ScanResult first = await service.RunAsync(null, TestContext.Current.CancellationToken);
        int raised = 0;
        service.LastResultChanged += (_, _) => raised++;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await service.RunAsync(null, cts.Token);

        Assert.Equal(1, raised);
        Assert.Same(first, service.LastResult);
    }

    [Fact]
    public async Task RunAsync_Cancelled_KeepsPreviousResult()
    {
        ScanService service = Create(new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 3)));
        ScanResult first = await service.RunAsync(null, TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        ScanResult second = await service.RunAsync(null, cts.Token);

        Assert.Equal(ScanCompletion.Cancelled, second.Completion);
        Assert.Same(first, service.LastResult);
    }

    [Fact]
    public async Task RunAsync_WhileRunning_Throws()
    {
        var reader = new FakeDirectoryReader().Folder(Root);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        reader.OnRead = _ =>
        {
            started.Set();
            release.Wait(TestContext.Current.CancellationToken);
        };
        ScanService service = Create(reader);

        Task<ScanResult> running = service.RunAsync(null, TestContext.Current.CancellationToken);
        started.Wait(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(null, TestContext.Current.CancellationToken));

        release.Set();
        await running;
    }
}

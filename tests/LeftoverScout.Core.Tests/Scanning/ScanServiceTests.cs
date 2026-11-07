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

    private static ScanService Create(FakeDirectoryReader reader) =>
        new(new ScanTarget(Root, true), reader, new NoPersonalFolders(), NullLogger<ScanService>.Instance, TimeProvider.System);

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

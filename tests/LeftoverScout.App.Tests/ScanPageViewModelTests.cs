using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.Tests;

public class ScanPageViewModelTests
{
    private sealed class StubScanService(Func<IProgress<ScanProgress>?, CancellationToken, Task<ScanResult>> run) : IScanService
    {
        public ScanResult? LastResult { get; private set; }

        public event EventHandler? LastResultChanged
        {
            add { }
            remove { }
        }

        public async Task<ScanResult> RunAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
        {
            ScanResult result = await run(progress, cancellationToken);
            if (result.IsCompleted)
            {
                LastResult = result;
            }

            return result;
        }
    }

    private static ScanResult CompletedResult()
    {
        var reader = new TinyReader();
        return new ProfileScanner(reader).ScanAsync(new ScanTarget(@"C:\Users\SampleUser", true), new ScanOptions(), null, CancellationToken.None).GetAwaiter().GetResult();
    }

    private sealed class TinyReader : IDirectoryReader
    {
        public DirectoryListing ReadDirectory(string path) =>
            path == @"C:\Users\SampleUser"
                ? DirectoryListing.Success([new DirectoryEntry("data.bin", false, 1536, DateTime.UnixEpoch, 0x20, 0)])
                : DirectoryListing.Success([]);
    }

    [Fact]
    public void Constructor_InitialState_AllowsStartButNotCancel()
    {
        var viewModel = new ScanPageViewModel(new StubScanService((_, _) => Task.FromResult(CompletedResult())));

        Assert.True(viewModel.StartScanCommand.CanExecute(null));
        Assert.False(viewModel.CancelScanCommand.CanExecute(null));
        Assert.False(viewModel.HasSummary);
    }

    [Fact]
    public async Task StartScan_Completed_ShowsSummaryAndStatus()
    {
        var viewModel = new ScanPageViewModel(new StubScanService((_, _) => Task.FromResult(CompletedResult())));

        await viewModel.StartScanCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsScanning);
        Assert.True(viewModel.HasSummary);
        Assert.StartsWith("Scan completed in ", viewModel.StatusMessage);
        SummaryItem local = Assert.Single(viewModel.SummaryItems, item => item.Label == "Local size");
        Assert.Equal(SizeFormatter.Format(1536), local.Value);
        Assert.Equal(string.Empty, viewModel.ResultMessage);
    }

    [Fact]
    public async Task StartScan_Cancelled_ShowsCancelledMessageWithoutTotals()
    {
        var viewModel = new ScanPageViewModel(new StubScanService(
            (_, _) => Task.FromResult(new ScanResult(ScanCompletion.Cancelled, null, [], [], 0, 0, 0, TimeSpan.Zero))));

        await viewModel.StartScanCommand.ExecuteAsync(null);

        Assert.Equal("Scan cancelled.", viewModel.ResultMessage);
        Assert.Equal("Scan cancelled", viewModel.StatusMessage);
        Assert.False(viewModel.HasSummary);
    }

    [Fact]
    public async Task CancelScan_WhileRunning_CancelsTheTokenAndSwitchesButtons()
    {
        var started = new TaskCompletionSource();
        var viewModel = new ScanPageViewModel(new StubScanService(async (_, token) =>
        {
            started.SetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {
                // Expected: the test cancels the scan.
            }

            return new ScanResult(ScanCompletion.Cancelled, null, [], [], 0, 0, 0, TimeSpan.Zero);
        }));

        Task running = viewModel.StartScanCommand.ExecuteAsync(null);
        await started.Task;

        Assert.True(viewModel.IsScanning);
        Assert.False(viewModel.StartScanCommand.CanExecute(null));
        Assert.True(viewModel.CancelScanCommand.CanExecute(null));
        Assert.Equal("Scanning…", viewModel.StatusMessage);

        viewModel.CancelScanCommand.Execute(null);
        await running;

        Assert.False(viewModel.IsScanning);
        Assert.Equal("Scan cancelled.", viewModel.ResultMessage);
        Assert.True(viewModel.StartScanCommand.CanExecute(null));
        Assert.False(viewModel.CancelScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task StartScan_ServiceThrows_ShowsFailureAndRecovers()
    {
        var viewModel = new ScanPageViewModel(new StubScanService((_, _) => throw new InvalidOperationException("boom")));

        await viewModel.StartScanCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsScanning);
        Assert.Equal("Scan failed", viewModel.StatusMessage);
        Assert.Contains("boom", viewModel.ResultMessage);
        Assert.True(viewModel.StartScanCommand.CanExecute(null));
    }

    [Fact]
    public async Task MainViewModel_FollowsScanPageStatus()
    {
        var scanPage = new ScanPageViewModel(new StubScanService((_, _) => Task.FromResult(CompletedResult())));
        var main = new MainViewModel([scanPage], new AppDataLocation(@"C:\Apps\SampleApp\data", IsFallback: false));
        Assert.Equal("Ready", main.StatusText);

        await scanPage.StartScanCommand.ExecuteAsync(null);

        Assert.StartsWith("Scan completed in ", main.StatusText);
    }
}

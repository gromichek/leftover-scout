using LeftoverScout.Core.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Scanning;

public class ProfileScannerTests
{
    private static readonly ScanTarget Target = new(Root, isCurrentUser: true);

    private static Task<ScanResult> Scan(
        FakeDirectoryReader reader,
        ScanOptions? options = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default,
        TimeProvider? time = null) =>
        new ProfileScanner(reader, time).ScanAsync(Target, options ?? new ScanOptions(), progress, cancellationToken);

    [Fact]
    public async Task ScanAsync_NestedFolders_AggregatesSizesCountsAndNewestDate()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("A"), DirEntry("B"), FileEntry("root.txt", 5, 2020))
            .Folder(Root + @"\A", DirEntry("Sub"), FileEntry("a.txt", 100, 2021))
            .Folder(Root + @"\A\Sub", FileEntry("deep.txt", 1000, 2024))
            .Folder(Root + @"\B", FileEntry("b.txt", 20, 2022));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsCompleted);
        FolderNode root = result.Root!;
        Assert.Equal(1125, root.LocalSize);
        Assert.Equal(4, root.FileCount);
        Assert.Equal(3, root.SubfolderCount);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), root.NewestWriteTimeUtc);

        FolderNode a = root.Children[0];
        Assert.Equal("A", a.Name);
        Assert.Equal(Root + @"\A", a.FullPath);
        Assert.Equal(1100, a.LocalSize);
        Assert.Equal(1, a.SubfolderCount);
        Assert.Equal(20, root.Children[1].LocalSize);
    }

    [Fact]
    public async Task ScanAsync_CloudOnlyFiles_AreCountedSeparately()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, FileEntry("local.bin", 10), CloudFileEntry("online.bin", 5000));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(10, result.Root!.LocalSize);
        Assert.Equal(5000, result.Root.CloudOnlySize);
        Assert.Equal(2, result.Root.FileCount);
    }

    [Fact]
    public async Task ScanAsync_Junctions_AreNotFollowedOrCounted()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, JunctionEntry("Application Data"), DirEntry("Real"))
            .Folder(Root + @"\Application Data", FileEntry("must-not-be-read.txt", 999))
            .Folder(Root + @"\Real", FileEntry("x.txt", 1));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.SkippedLinkCount);
        Assert.Equal(1, result.Root!.LocalSize);
        Assert.Equal(1, result.Root.SubfolderCount);
        Assert.DoesNotContain(Root + @"\Application Data", reader.ReadPaths, StringComparer.OrdinalIgnoreCase);
        FolderNode link = Assert.Single(result.Root.Children, child => child.IsSkippedLink);
        Assert.Equal("Application Data", link.Name);
        Assert.Equal(0, link.LocalSize);
    }

    [Fact]
    public async Task ScanAsync_CloudFolderNotPopulated_IsNotEnumerated()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, CloudDirEntry("CloudOnly"))
            .Folder(Root + @"\CloudOnly", FileEntry("never.txt", 50));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.CloudFoldersNotScannedCount);
        Assert.DoesNotContain(Root + @"\CloudOnly", reader.ReadPaths, StringComparer.OrdinalIgnoreCase);
        Assert.True(result.Root!.Children[0].IsCloudFolderNotScanned);
        Assert.Equal(0, result.Root.LocalSize);
        Assert.Equal(1, result.Root.SubfolderCount);
    }

    [Fact]
    public async Task ScanAsync_AccessDenied_IsRecordedAndScanContinues()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Locked"), DirEntry("Open"))
            .Failing(Root + @"\Locked", "Access denied", accessDenied: true)
            .Folder(Root + @"\Open", FileEntry("ok.txt", 7));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsCompleted);
        Assert.Equal(7, result.Root!.LocalSize);
        ScanError error = Assert.Single(result.Errors);
        Assert.Equal(Root + @"\Locked", error.Path);
        Assert.Equal("Access denied", error.Reason);
        FolderNode locked = result.Root.Children[0];
        Assert.True(locked.IsAccessDenied);
        Assert.False(locked.HasError);
    }

    [Fact]
    public async Task ScanAsync_OtherError_SetsHasError()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Broken"))
            .Failing(Root + @"\Broken", "The path is invalid", accessDenied: false);

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        FolderNode broken = result.Root!.Children[0];
        Assert.True(broken.HasError);
        Assert.False(broken.IsAccessDenied);
    }

    [Fact]
    public async Task ScanAsync_PersonalFolders_AreMarkedAndSummedOnce()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Documents"), DirEntry("OneDrive"), DirEntry("AppData"))
            .Folder(Root + @"\Documents", FileEntry("d.txt", 10))
            .Folder(Root + @"\OneDrive", DirEntry("Documents"), FileEntry("o.txt", 20))
            .Folder(Root + @"\OneDrive\Documents", FileEntry("od.txt", 30))
            .Folder(Root + @"\AppData", FileEntry("cache.bin", 1000));
        var options = new ScanOptions
        {
            PersonalFolderPaths = [Root + @"\Documents", Root + @"\OneDrive", Root + @"\OneDrive\Documents", @"D:\Elsewhere\Pictures"],
        };

        ScanResult result = await Scan(reader, options, cancellationToken: TestContext.Current.CancellationToken);

        FolderNode root = result.Root!;
        Assert.False(root.Children.Single(c => c.Name == "AppData").IsPersonal);
        Assert.True(root.Children.Single(c => c.Name == "Documents").IsPersonal);
        FolderNode oneDrive = root.Children.Single(c => c.Name == "OneDrive");
        Assert.True(oneDrive.IsPersonal);
        Assert.True(oneDrive.Children.Single().IsPersonal);
        Assert.Equal(10 + 50, result.PersonalLocalSize);
    }

    [Fact]
    public async Task ScanAsync_LargeFiles_ListedAtOrAboveThresholdLargestFirst()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("A"), FileEntry("small.bin", 99), FileEntry("exact.bin", 100), CloudFileEntry("cloud-big.bin", 5000))
            .Folder(Root + @"\A", FileEntry("big.bin", 300));
        var options = new ScanOptions { LargeFileThresholdBytes = 100 };

        ScanResult result = await Scan(reader, options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { Root + @"\A\big.bin", Root + @"\exact.bin" }, result.LargeFiles.Select(f => f.Path));
        Assert.Equal(new[] { 300L, 100L }, result.LargeFiles.Select(f => f.Size));
    }

    [Fact]
    public async Task ScanAsync_Children_AreSortedByNameIgnoringCase()
    {
        var reader = new FakeDirectoryReader().Folder(Root, DirEntry("zeta"), DirEntry("Beta"), DirEntry("alpha"), DirEntry("Gamma"));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "alpha", "Beta", "Gamma", "zeta" }, result.Root!.Children.Select(c => c.Name));
    }

    [Fact]
    public async Task ScanAsync_CancelledBeforeStart_ReturnsCancelledWithoutTotals()
    {
        var reader = new FakeDirectoryReader().Folder(Root, FileEntry("a.txt", 1));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        ScanResult result = await Scan(reader, cancellationToken: cts.Token);

        Assert.Equal(ScanCompletion.Cancelled, result.Completion);
        Assert.Null(result.Root);
        Assert.Empty(reader.ReadPaths);
    }

    [Fact]
    public async Task ScanAsync_CancelledDuringScan_StopsReadingFolders()
    {
        var reader = new FakeDirectoryReader().Folder(Root, DirEntry("A"), DirEntry("B"), DirEntry("C"));
        using var cts = new CancellationTokenSource();
        reader.OnRead = path =>
        {
            if (path.EndsWith(@"\A", StringComparison.OrdinalIgnoreCase) || path.EndsWith(@"\B", StringComparison.OrdinalIgnoreCase) || path.EndsWith(@"\C", StringComparison.OrdinalIgnoreCase))
            {
                cts.Cancel();
            }
        };

        ScanResult result = await Scan(reader, cancellationToken: cts.Token);

        Assert.Equal(ScanCompletion.Cancelled, result.Completion);
        Assert.Null(result.Root);
        Assert.Equal(2, reader.ReadPaths.Count); // root plus the first subfolder, then the token is seen
    }

    [Fact]
    public async Task ScanAsync_Progress_IsThrottled()
    {
        var time = new ManualTimeProvider();
        var reader = new FakeDirectoryReader();
        var rootEntries = new List<DirectoryEntry>();
        for (int i = 0; i < 100; i++)
        {
            rootEntries.Add(DirEntry("F" + i));
        }

        reader.Folder(Root, [.. rootEntries]);
        reader.OnRead = _ => time.Advance(TimeSpan.FromMilliseconds(10));
        var reports = new List<ScanProgress>();
        var progress = new SynchronousProgress(reports);

        await Scan(reader, new ScanOptions { ProgressInterval = TimeSpan.FromMilliseconds(100) }, progress, TestContext.Current.CancellationToken, time);

        // 101 folders at 10 ms each is about one second: roughly ten reports plus the first one.
        Assert.InRange(reports.Count, 5, 15);
        Assert.Equal(Root, reports[0].CurrentFolder);
    }

    [Fact]
    public async Task ScanAsync_Duration_UsesTheClock()
    {
        var time = new ManualTimeProvider();
        var reader = new FakeDirectoryReader().Folder(Root);
        reader.OnRead = _ => time.Advance(TimeSpan.FromSeconds(3));

        ScanResult result = await Scan(reader, cancellationToken: TestContext.Current.CancellationToken, time: time);

        Assert.Equal(TimeSpan.FromSeconds(3), result.Duration);
    }

    private sealed class SynchronousProgress(List<ScanProgress> reports) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value) => reports.Add(value);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref _ticks);

        public void Advance(TimeSpan amount) => Interlocked.Add(ref _ticks, amount.Ticks);
    }
}

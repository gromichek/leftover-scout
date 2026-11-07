using LeftoverScout.Core.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Scanning;

public class ScanSummaryTests
{
    [Fact]
    public async Task From_CompletedScan_ListsLargestTopLevelFoldersAndTotals()
    {
        var reader = new FakeDirectoryReader().Folder(Root, JunctionEntry("Application Data"), FileEntry("loose.txt", 1));
        var entries = new List<DirectoryEntry> { JunctionEntry("Application Data"), FileEntry("loose.txt", 1) };
        for (int i = 1; i <= 12; i++)
        {
            entries.Add(DirEntry("Folder" + i));
            reader.Folder(Root + @"\Folder" + i, FileEntry("f.bin", i * 10));
        }

        reader.Folder(Root, [.. entries]);
        ScanResult result = await new ProfileScanner(reader).ScanAsync(new ScanTarget(Root, true), new ScanOptions(), null, TestContext.Current.CancellationToken);

        ScanSummary summary = ScanSummary.From(result);

        Assert.Equal(ScanSummary.TopFolderCount, summary.LargestTopLevelFolders.Count);
        Assert.Equal("Folder12", summary.LargestTopLevelFolders[0].Name);
        Assert.Equal(120, summary.LargestTopLevelFolders[0].LocalSize);
        Assert.DoesNotContain(summary.LargestTopLevelFolders, f => f.Name == "Application Data");
        Assert.Equal(1 + 10 * (12 * 13 / 2), summary.LocalSize);
        Assert.Equal(12, summary.FolderCount);
        Assert.Equal(13, summary.FileCount);
        Assert.Equal(1, summary.SkippedLinkCount);
    }

    [Fact]
    public void From_CancelledScan_Throws()
    {
        var cancelled = new ScanResult(ScanCompletion.Cancelled, null, [], [], 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => ScanSummary.From(cancelled));
    }
}

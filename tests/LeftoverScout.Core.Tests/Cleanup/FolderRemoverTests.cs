using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Tests.Scanning;

namespace LeftoverScout.Core.Tests.Cleanup;

public class FolderRemoverTests
{
    private const string Root = FakeDirectoryReader.Root;
    private const string Folder = Root + @"\OldApp";

    private static FolderRemover Remover(FakeEntryRemover entries, FakeDirectoryReader reader) => new(entries, reader);

    [Fact]
    public void Remove_WholeFolderWorks_NoFallback()
    {
        var entries = new FakeEntryRemover();

        FolderRemoveResult result = Remover(entries, new FakeDirectoryReader()).Remove(Folder, CleanupMode.RecycleBin);

        Assert.Equal(FolderRemoveStatus.Removed, result.Status);
        Assert.Equal([Folder], entries.Calls);
    }

    [Fact]
    public void Remove_OneLockedFile_RemovesTheRestAndReportsPartly()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Folder, FakeDirectoryReader.FileEntry("a.bin", 1), FakeDirectoryReader.FileEntry("locked.bin", 1), FakeDirectoryReader.DirEntry("sub"))
            .Folder(Folder + @"\sub", FakeDirectoryReader.FileEntry("b.bin", 1));
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder + @"\locked.bin");

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.Permanent);

        Assert.Equal(FolderRemoveStatus.PartlyRemoved, result.Status);
        Assert.Equal(1, result.ItemsLeft);
        Assert.Contains(Folder + @"\a.bin", entries.Gone);
        Assert.Contains(Folder + @"\sub", entries.Gone);
        Assert.DoesNotContain(Folder + @"\locked.bin", entries.Gone);
        Assert.DoesNotContain(Folder, entries.Gone);
    }

    [Fact]
    public void Remove_LockedFileInNestedFolder_GoesDepthFirstAndKeepsOnlyTheParents()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Folder, FakeDirectoryReader.DirEntry("sub"), FakeDirectoryReader.FileEntry("a.bin", 1))
            .Folder(Folder + @"\sub", FakeDirectoryReader.FileEntry("x.bin", 1), FakeDirectoryReader.FileEntry("locked.bin", 1));
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder + @"\sub\locked.bin");

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.RecycleBin);

        Assert.Equal(FolderRemoveStatus.PartlyRemoved, result.Status);
        Assert.Equal(1, result.ItemsLeft);
        Assert.Contains(Folder + @"\sub\x.bin", entries.Gone);
        Assert.Contains(Folder + @"\a.bin", entries.Gone);
        Assert.DoesNotContain(Folder + @"\sub", entries.Gone);
    }

    [Fact]
    public void Remove_EverythingLocked_IsFailedNotPartly()
    {
        var reader = new FakeDirectoryReader().Folder(Folder, FakeDirectoryReader.FileEntry("locked.bin", 1));
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder + @"\locked.bin");

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.Permanent);

        Assert.Equal(FolderRemoveStatus.Failed, result.Status);
        Assert.Equal(1, result.ItemsLeft);
    }

    [Fact]
    public void Remove_VetoedWholeFolder_IsNotRecycledAndChildrenAreNotTried()
    {
        var entries = new FakeEntryRemover();
        entries.Vetoed.Add(Folder);
        var reader = new FakeDirectoryReader().Folder(Folder, FakeDirectoryReader.FileEntry("a.bin", 1));

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.RecycleBin);

        Assert.Equal(FolderRemoveStatus.NotRecycled, result.Status);
        Assert.Equal([Folder], entries.Calls);
    }

    [Fact]
    public void Remove_JunctionInsideLockedFolder_IsLeftInPlaceAndNeverTouched()
    {
        var reader = new FakeDirectoryReader().Folder(
            Folder,
            FakeDirectoryReader.JunctionEntry("link"),
            FakeDirectoryReader.FileEntry("a.bin", 1),
            FakeDirectoryReader.FileEntry("locked.bin", 1));
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder + @"\locked.bin");

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.Permanent);

        Assert.Equal(FolderRemoveStatus.PartlyRemoved, result.Status);
        Assert.Equal(2, result.ItemsLeft);
        Assert.DoesNotContain(Folder + @"\link", entries.Calls);
    }

    [Fact]
    public void Remove_CloudFolderInside_IsNotTouched()
    {
        var reader = new FakeDirectoryReader().Folder(
            Folder,
            FakeDirectoryReader.CloudDirEntry("cloud"),
            FakeDirectoryReader.FileEntry("locked.bin", 1),
            FakeDirectoryReader.FileEntry("a.bin", 1));
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder + @"\locked.bin");

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.Permanent);

        Assert.DoesNotContain(Folder + @"\cloud", entries.Calls);
        Assert.Equal(FolderRemoveStatus.PartlyRemoved, result.Status);
    }

    [Fact]
    public void Remove_UnexpectedPermanentDeletionOfAChild_IsReported()
    {
        var reader = new FakeDirectoryReader().Folder(Folder, FakeDirectoryReader.FileEntry("a.bin", 1), FakeDirectoryReader.FileEntry("locked.bin", 1));
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder + @"\locked.bin");
        entries.Unexpected.Add(Folder + @"\a.bin");

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.RecycleBin);

        Assert.True(result.DeletedPermanentlyUnexpectedly);
    }

    [Fact]
    public void Remove_UnreadableFolderAfterFailure_IsFailed()
    {
        var reader = new FakeDirectoryReader().Failing(Folder, "Access denied", accessDenied: true);
        var entries = new FakeEntryRemover();
        entries.Locked.Add(Folder);

        FolderRemoveResult result = Remover(entries, reader).Remove(Folder, CleanupMode.Permanent);

        Assert.Equal(FolderRemoveStatus.Failed, result.Status);
    }
}

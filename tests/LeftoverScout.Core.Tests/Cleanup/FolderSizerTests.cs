using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Tests.Scanning;

namespace LeftoverScout.Core.Tests.Cleanup;

public class FolderSizerTests
{
    private const string Folder = FakeDirectoryReader.Root + @"\OldApp";

    [Fact]
    public void Measure_SumsNestedFilesAndSkipsLinksAndCloudFolders()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Folder, FakeDirectoryReader.FileEntry("a", 10), FakeDirectoryReader.DirEntry("sub"), FakeDirectoryReader.JunctionEntry("link"), FakeDirectoryReader.CloudDirEntry("cloud"))
            .Folder(Folder + @"\sub", FakeDirectoryReader.FileEntry("b", 5))
            .Folder(Folder + @"\link", FakeDirectoryReader.FileEntry("must-not-count", 1000))
            .Folder(Folder + @"\cloud", FakeDirectoryReader.FileEntry("must-not-count", 1000));

        Assert.Equal(15, FolderSizer.Measure(reader, Folder));
        Assert.DoesNotContain(Folder + @"\link", reader.ReadPaths);
        Assert.DoesNotContain(Folder + @"\cloud", reader.ReadPaths);
    }
}

using LeftoverScout.Windows.Storage;

namespace LeftoverScout.Windows.Tests.Storage;

public class WritableFolderProbeTests
{
    [Fact]
    public void TryEnsureWritable_FolderMissing_CreatesItAndLeavesItEmpty()
    {
        string root = Path.Combine(Path.GetTempPath(), "LeftoverScoutTests-" + Guid.NewGuid().ToString("N"));
        string folder = Path.Combine(root, "data");
        try
        {
            bool result = new WritableFolderProbe().TryEnsureWritable(folder);

            Assert.True(result);
            Assert.True(Directory.Exists(folder));
            Assert.Empty(Directory.GetFileSystemEntries(folder));
        }
        finally
        {
            // Temp folder created by this test.
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void TryEnsureWritable_PathIsAFile_ReturnsFalse()
    {
        string file = Path.Combine(Path.GetTempPath(), "LeftoverScoutTests-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(file, "x");
        try
        {
            Assert.False(new WritableFolderProbe().TryEnsureWritable(file));
        }
        finally
        {
            File.Delete(file);
        }
    }
}

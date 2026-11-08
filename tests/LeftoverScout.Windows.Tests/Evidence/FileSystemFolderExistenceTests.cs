using LeftoverScout.Windows.Shell;

namespace LeftoverScout.Windows.Tests.Evidence;

public class FileSystemFolderExistenceTests
{
    [Fact]
    public void FolderExists_ExistingTempFolder_ReturnsTrue_AndFileOrMissingReturnsFalse()
    {
        DirectoryInfo temp = Directory.CreateTempSubdirectory("LeftoverScoutTests-");
        try
        {
            string file = System.IO.Path.Combine(temp.FullName, "file.txt");
            File.WriteAllText(file, "x");
            var check = new FileSystemFolderExistence();

            Assert.True(check.FolderExists(temp.FullName));
            Assert.False(check.FolderExists(file));
            Assert.False(check.FolderExists(System.IO.Path.Combine(temp.FullName, "missing")));
        }
        finally
        {
            // The folder was created by this test in the system temp folder.
            temp.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\0name")]
    public void FolderExists_InvalidPath_ReturnsFalse(string path)
    {
        Assert.False(new FileSystemFolderExistence().FolderExists(path));
    }
}

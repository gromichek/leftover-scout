using System.Diagnostics;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Windows.Scanning;

namespace LeftoverScout.Windows.Tests.Scanning;

public class Win32DirectoryReaderTests
{
    private readonly Win32DirectoryReader _reader = new();

    [Fact]
    public void ReadDirectory_FilesAndFolders_ReturnsNamesSizesAndKinds()
    {
        using var temp = new WindowsTempFolder();
        File.WriteAllBytes(Path.Combine(temp.Path, "data.bin"), new byte[1234]);
        Directory.CreateDirectory(Path.Combine(temp.Path, "Sub"));

        DirectoryListing listing = _reader.ReadDirectory(temp.Path);

        Assert.Null(listing.ErrorReason);
        Assert.Equal(new[] { "Sub", "data.bin" }, listing.Entries.Select(e => e.Name).Order(StringComparer.Ordinal).ToArray());
        DirectoryEntry file = Assert.Single(listing.Entries, e => !e.IsDirectory);
        Assert.Equal(1234, file.Length);
        Assert.Equal(0u, file.ReparseTag);
        Assert.True(Math.Abs((file.LastWriteTimeUtc - DateTime.UtcNow).TotalMinutes) < 10);
        Assert.Equal(EntryKind.File, EntryClassifier.Classify(file));
        DirectoryEntry folder = Assert.Single(listing.Entries, e => e.IsDirectory);
        Assert.Equal(EntryKind.Directory, EntryClassifier.Classify(folder));
    }

    [Fact]
    public void ReadDirectory_EmptyFolder_ReturnsNoEntries()
    {
        using var temp = new WindowsTempFolder();

        DirectoryListing listing = _reader.ReadDirectory(temp.Path);

        Assert.Null(listing.ErrorReason);
        Assert.Empty(listing.Entries);
    }

    [Fact]
    public void ReadDirectory_MissingFolder_ReturnsErrorAsData()
    {
        using var temp = new WindowsTempFolder();

        DirectoryListing listing = _reader.ReadDirectory(Path.Combine(temp.Path, "does-not-exist"));

        Assert.NotNull(listing.ErrorReason);
        Assert.False(listing.IsAccessDenied);
        Assert.Empty(listing.Entries);
    }

    [Fact]
    public void ReadDirectory_PathLongerThan260Characters_Works()
    {
        using var temp = new WindowsTempFolder();
        string deep = temp.Path;
        for (int i = 0; i < 6; i++)
        {
            deep = Path.Combine(deep, new string((char)('a' + i), 50));
        }

        Directory.CreateDirectory(deep);
        File.WriteAllBytes(Path.Combine(deep, "deep.txt"), new byte[10]);
        Assert.True(deep.Length > 260);

        DirectoryListing listing = _reader.ReadDirectory(deep);

        Assert.Null(listing.ErrorReason);
        Assert.Equal("deep.txt", Assert.Single(listing.Entries).Name);
    }

    [Fact]
    public void ReadDirectory_Junction_IsReportedAsSkippedLink()
    {
        using var temp = new WindowsTempFolder();
        string target = Path.Combine(temp.Path, "target");
        string link = Path.Combine(temp.Path, "link");
        Directory.CreateDirectory(target);
        File.WriteAllBytes(Path.Combine(target, "inside.txt"), new byte[5]);

        if (!TryCreateJunction(link, target))
        {
            Assert.Skip("A directory junction could not be created in this environment.");
        }

        try
        {
            DirectoryListing listing = _reader.ReadDirectory(temp.Path);

            DirectoryEntry linkEntry = Assert.Single(listing.Entries, e => e.Name == "link");
            Assert.Equal(EntryClassifier.ReparseTagMountPoint, linkEntry.ReparseTag);
            Assert.Equal(EntryKind.SkippedLink, EntryClassifier.Classify(linkEntry));
        }
        finally
        {
            // Removes only the junction itself (non-recursive), never the target's content.
            Directory.Delete(link);
        }
    }

    [Theory]
    [InlineData(@"C:\Users\SampleUser", @"\\?\C:\Users\SampleUser")]
    [InlineData(@"C:\Users\SampleUser\", @"\\?\C:\Users\SampleUser")]
    [InlineData(@"C:\", @"\\?\C:\")]
    [InlineData(@"\\server\share\folder", @"\\?\UNC\server\share\folder")]
    [InlineData(@"\\?\C:\Already", @"\\?\C:\Already")]
    public void ToExtendedPath_AddsPrefix(string path, string expected)
    {
        Assert.Equal(expected, Win32DirectoryReader.ToExtendedPath(path));
    }

    private static bool TryCreateJunction(string link, string target)
    {
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("mklink");
        startInfo.ArgumentList.Add("/J");
        startInfo.ArgumentList.Add(link);
        startInfo.ArgumentList.Add(target);

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 && Directory.Exists(link);
    }
}

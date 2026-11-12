using LeftoverScout.Core.Journal;

namespace LeftoverScout.Core.Tests.Journal;

public class FileJournalStorageTests
{
    [Fact]
    public void WriteAtomic_CreatesFolderAndFile_AndLeavesNoTemporaryFile()
    {
        using var temp = new TempFolder();
        var storage = new FileJournalStorage(temp.Path);

        storage.WriteAtomic("cleanup-20261112-103000-abcd1234.json", "{}");
        storage.WriteAtomic("cleanup-20261112-103000-abcd1234.json", "{ \"a\": 1 }");

        string[] files = Directory.GetFiles(storage.FolderPath);
        Assert.Single(files);
        Assert.Equal("{ \"a\": 1 }", File.ReadAllText(files[0]));
        Assert.EndsWith("journal", storage.FolderPath, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteAtomic_WritesUtf8WithoutByteOrderMark()
    {
        using var temp = new TempFolder();
        var storage = new FileJournalStorage(temp.Path);

        storage.WriteAtomic("cleanup-20261112-103000-abcd1234.json", "x");

        byte[] bytes = File.ReadAllBytes(Path.Combine(storage.FolderPath, "cleanup-20261112-103000-abcd1234.json"));
        Assert.Equal(new byte[] { (byte)'x' }, bytes);
    }

    [Fact]
    public void ListFileNames_MissingFolder_IsEmpty()
    {
        using var temp = new TempFolder();

        Assert.Empty(new FileJournalStorage(temp.Path).ListFileNames());
    }

    [Fact]
    public void ListFileNames_ReturnsOnlyJournalFiles()
    {
        using var temp = new TempFolder();
        var storage = new FileJournalStorage(temp.Path);
        storage.WriteAtomic("cleanup-20261112-103000-abcd1234.json", "{}");
        File.WriteAllText(Path.Combine(storage.FolderPath, "notes.txt"), "x");
        File.WriteAllText(Path.Combine(storage.FolderPath, "cleanup-x.json.tmp"), "x");

        Assert.Equal(["cleanup-20261112-103000-abcd1234.json"], storage.ListFileNames());
    }

    [Fact]
    public void EnsureFolder_CreatesTheFolder()
    {
        using var temp = new TempFolder();
        var storage = new FileJournalStorage(temp.Path);

        Assert.True(storage.EnsureFolder());
        Assert.True(Directory.Exists(storage.FolderPath));
    }

    [Fact]
    public void JournalFileName_RoundTripsTime()
    {
        string name = JournalFileName.Create(new DateTimeOffset(2026, 11, 12, 10, 30, 5, TimeSpan.FromHours(2)), Guid.Parse("abcd1234-0000-0000-0000-000000000000"));

        Assert.Equal("cleanup-20261112-103005-abcd1234.json", name);
        Assert.Equal(new DateTime(2026, 11, 12, 10, 30, 5), JournalFileName.TryParseTime(name));
        Assert.Null(JournalFileName.TryParseTime("something.json"));
    }
}

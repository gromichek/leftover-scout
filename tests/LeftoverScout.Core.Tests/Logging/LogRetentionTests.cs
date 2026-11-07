using LeftoverScout.Core.Logging;

namespace LeftoverScout.Core.Tests.Logging;

public class LogRetentionTests
{
    [Fact]
    public void Apply_MoreThanSevenFiles_KeepsSevenNewest()
    {
        using var temp = new TempFolder();
        for (int day = 1; day <= 10; day++)
        {
            File.WriteAllText(Path.Combine(temp.Path, $"leftoverscout-202611{day:00}.log"), "x");
        }

        LogRetention.Apply(temp.Path);

        string[] remaining = Directory.GetFiles(temp.Path).Select(file => Path.GetFileName(file)).Order(StringComparer.Ordinal).ToArray();
        string[] expected =
        {
            "leftoverscout-20261104.log", "leftoverscout-20261105.log", "leftoverscout-20261106.log", "leftoverscout-20261107.log",
            "leftoverscout-20261108.log", "leftoverscout-20261109.log", "leftoverscout-20261110.log",
        };
        Assert.Equal(expected, remaining);
    }

    [Fact]
    public void Apply_FilesWithOtherNames_AreNeverDeleted()
    {
        using var temp = new TempFolder();
        for (int day = 1; day <= 9; day++)
        {
            File.WriteAllText(Path.Combine(temp.Path, $"leftoverscout-202611{day:00}.log"), "x");
        }

        string[] others = ["notes.txt", "otherapp-20200101.log", "leftoverscout-latest.log", "leftoverscout-20200101.log.bak"];
        foreach (string other in others)
        {
            File.WriteAllText(Path.Combine(temp.Path, other), "x");
        }

        LogRetention.Apply(temp.Path);

        foreach (string other in others)
        {
            Assert.True(File.Exists(Path.Combine(temp.Path, other)), other);
        }

        Assert.Equal(7, Directory.GetFiles(temp.Path, "leftoverscout-2026*.log").Length);
    }

    [Fact]
    public void Apply_FolderMissing_DoesNotThrow()
    {
        using var temp = new TempFolder();

        LogRetention.Apply(Path.Combine(temp.Path, "missing"));
    }
}

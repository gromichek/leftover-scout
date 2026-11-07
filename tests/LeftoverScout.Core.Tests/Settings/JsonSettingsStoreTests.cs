using LeftoverScout.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Core.Tests.Settings;

public class JsonSettingsStoreTests
{
    private static JsonSettingsStore CreateStore(string folder) => new(folder, NullLogger<JsonSettingsStore>.Instance);

    [Fact]
    public void SaveThenLoad_RoundTripsSettings()
    {
        using var temp = new TempFolder();
        var settings = new AppSettings(MainWindow: new WindowPlacement(10, 20, 900, 600, IsMaximized: true));

        CreateStore(temp.Path).Save(settings);
        AppSettings loaded = CreateStore(temp.Path).Load();

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public void Load_FileMissing_ReturnsDefaults()
    {
        using var temp = new TempFolder();

        AppSettings loaded = CreateStore(temp.Path).Load();

        Assert.Equal(new AppSettings(), loaded);
        Assert.Null(loaded.MainWindow);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("")]
    [InlineData("{ \"SchemaVersion\": ")]
    [InlineData("null")]
    public void Load_FileCorrupt_ReturnsDefaultsWithoutThrowing(string content)
    {
        using var temp = new TempFolder();
        File.WriteAllText(Path.Combine(temp.Path, JsonSettingsStore.FileName), content);

        AppSettings loaded = CreateStore(temp.Path).Load();

        Assert.Equal(new AppSettings(), loaded);
    }

    [Fact]
    public void Save_LeavesNoTemporaryFile()
    {
        using var temp = new TempFolder();
        JsonSettingsStore store = CreateStore(temp.Path);

        store.Save(new AppSettings());
        store.Save(new AppSettings(MainWindow: new WindowPlacement(1, 2, 3, 4, false)));

        string[] files = Directory.GetFiles(temp.Path);
        Assert.Equal(new[] { Path.Combine(temp.Path, JsonSettingsStore.FileName) }, files);
    }

    [Fact]
    public void Save_FolderMissing_CreatesIt()
    {
        using var temp = new TempFolder();
        string dataFolder = Path.Combine(temp.Path, "data");

        CreateStore(dataFolder).Save(new AppSettings());

        Assert.True(File.Exists(Path.Combine(dataFolder, JsonSettingsStore.FileName)));
    }

    [Fact]
    public void Save_TargetIsUnusable_DoesNotThrow()
    {
        using var temp = new TempFolder();

        // A directory named settings.json makes the replace step fail.
        Directory.CreateDirectory(Path.Combine(temp.Path, JsonSettingsStore.FileName));

        CreateStore(temp.Path).Save(new AppSettings());

        Assert.Single(Directory.GetFileSystemEntries(temp.Path));
    }
}

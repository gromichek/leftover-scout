using LeftoverScout.Core.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Core.Tests.Settings;

public class AppSettingsHelpLanguageTests
{
    [Fact]
    public void HelpLanguage_SavedAndLoaded_RoundTrips()
    {
        using var temp = new TempFolder();
        var store = new JsonSettingsStore(temp.Path, NullLogger<JsonSettingsStore>.Instance);

        store.Save(new AppSettings(HelpLanguage: "ru"));

        Assert.Equal("ru", store.Load().HelpLanguage);
    }

    [Fact]
    public void HelpLanguage_OldFileWithoutIt_IsAutomatic()
    {
        using var temp = new TempFolder();
        File.WriteAllText(System.IO.Path.Combine(temp.Path, JsonSettingsStore.FileName), "{ \"SchemaVersion\": 1 }");
        var store = new JsonSettingsStore(temp.Path, NullLogger<JsonSettingsStore>.Instance);

        Assert.Null(store.Load().HelpLanguage);
    }
}

using System.Globalization;
using LeftoverScout.Core.Help;

namespace LeftoverScout.Core.Tests.Help;

public class HelpLanguagesTests
{
    [Theory]
    [InlineData("ru-RU", HelpLanguage.Russian)]
    [InlineData("ru", HelpLanguage.Russian)]
    [InlineData("en-US", HelpLanguage.English)]
    [InlineData("de-DE", HelpLanguage.English)]
    [InlineData("uk-UA", HelpLanguage.English)]
    public void Resolve_NoSetting_FollowsUiLanguage(string culture, HelpLanguage expected)
    {
        Assert.Equal(expected, HelpLanguages.Resolve(null, new CultureInfo(culture)));
    }

    [Theory]
    [InlineData("en", "ru-RU", HelpLanguage.English)]
    [InlineData("ru", "en-US", HelpLanguage.Russian)]
    [InlineData("RU", "en-US", HelpLanguage.Russian)]
    public void Resolve_ExplicitSetting_WinsOverUiLanguage(string setting, string culture, HelpLanguage expected)
    {
        Assert.Equal(expected, HelpLanguages.Resolve(setting, new CultureInfo(culture)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    [InlineData("garbage")]
    public void Resolve_UnknownSetting_FallsBackToAutomatic(string setting)
    {
        Assert.Equal(HelpLanguage.Russian, HelpLanguages.Resolve(setting, new CultureInfo("ru-RU")));
        Assert.Equal(HelpLanguage.English, HelpLanguages.Resolve(setting, new CultureInfo("en-US")));
    }

    [Fact]
    public void ToCode_ReturnsFolderNames()
    {
        Assert.Equal("en", HelpLanguages.ToCode(HelpLanguage.English));
        Assert.Equal("ru", HelpLanguages.ToCode(HelpLanguage.Russian));
    }
}

using LeftoverScout.Core.Classification;

namespace LeftoverScout.Core.Tests.Classification;

public class NameNormalizerTests
{
    [Theory]
    [InlineData("Visual Studio Code", "visual studio code")]
    [InlineData("discord", "discord")]
    [InlineData("JetBrains s.r.o.", "jet brains")]
    [InlineData("JetBrains S.R.O.", "jet brains")]
    [InlineData("Contoso Inc.", "contoso")]
    [InlineData("Contoso, Inc", "contoso")]
    [InlineData("Contoso Co., Ltd.", "contoso")]
    [InlineData("Contoso Corporation", "contoso")]
    [InlineData("Contoso GmbH", "contoso")]
    [InlineData("Contoso AG", "contoso")]
    [InlineData("Fabrikam LLC", "fabrikam")]
    [InlineData("Fabrikam L.L.C.", "fabrikam")]
    [InlineData("Fabrikam Corp", "fabrikam")]
    [InlineData("Sample App 2022", "sample app")]
    [InlineData("Sample App v2", "sample app")]
    [InlineData("Sample App (x64)", "sample app")]
    [InlineData("Sample App x86", "sample app")]
    [InlineData("FooSoft_v2", "foo soft")]
    [InlineData("Foo 1.2.3 Soft", "foo soft")]
    [InlineData("Python3", "python")]
    [InlineData("PyCharm2020.3", "py charm")]
    [InlineData("JSONParser", "json parser")]
    [InlineData("my-app.name_here", "my app name here")]
    [InlineData("Contoso.App_abc123xyz", "contoso app abc xyz")]
    [InlineData("  spaced   out  ", "spaced out")]
    public void Normalize_Text_GivesExpectedTokens(string text, string expectedTokens)
    {
        NormalizedName name = NameNormalizer.Normalize(text);

        Assert.Equal(expectedTokens, string.Join(' ', name.Tokens));
    }

    [Fact]
    public void Normalize_JoinedForm_HasNoSeparators()
    {
        Assert.Equal("visualstudiocode", NameNormalizer.Normalize("Visual Studio Code").Joined);
        Assert.Equal("jetbrains", NameNormalizer.Normalize("JetBrains").Joined);
        Assert.Equal(NameNormalizer.Normalize("Foo Soft").Joined, NameNormalizer.Normalize("FooSoft").Joined);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2022")]
    [InlineData("v2")]
    [InlineData("x64")]
    [InlineData("Inc.")]
    [InlineData("---")]
    public void Normalize_NothingLeft_IsEmpty(string? text)
    {
        NormalizedName name = NameNormalizer.Normalize(text);

        Assert.True(name.IsEmpty);
        Assert.Equal(string.Empty, name.Joined);
    }
}

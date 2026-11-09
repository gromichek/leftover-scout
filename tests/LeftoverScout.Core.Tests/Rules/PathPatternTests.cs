using LeftoverScout.Core.Rules;

namespace LeftoverScout.Core.Tests.Rules;

public class PathPatternTests
{
    private static PathPattern Parse(string text)
    {
        Assert.True(PathPattern.TryParse(text, out PathPattern? pattern, out string? error), error);
        return pattern;
    }

    [Theory]
    [InlineData(@"{Profile}\.nuget\packages", new[] { ".nuget", "packages" })]
    [InlineData(@"{RoamingAppData}\discord", new[] { "AppData", "Roaming", "discord" })]
    [InlineData(@"{LocalAppData}\Temp", new[] { "AppData", "Local", "Temp" })]
    [InlineData(@"{LocalLow}\NVIDIA", new[] { "AppData", "LocalLow", "NVIDIA" })]
    [InlineData(@"{LocalAppData}\Google\Chrome\User Data\*\Cache", new[] { "AppData", "Local", "Google", "Chrome", "User Data", "*", "Cache" })]
    [InlineData("{Profile}", new string[0])]
    public void TryParse_ValidPattern_ExpandsTheToken(string text, string[] segments)
    {
        Assert.Equal(segments, Parse(text).Segments);
    }

    [Fact]
    public void LiteralCount_CountsNamesButNotWildcards()
    {
        Assert.Equal(6, Parse(@"{LocalAppData}\Google\Chrome\User Data\*\Cache").LiteralCount);
        Assert.Equal(2, Parse(@"{Profile}\a\*\b").LiteralCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"C:\Users\SampleUser\AppData")]
    [InlineData(@"\AppData\Local")]
    [InlineData(@"AppData\Local")]
    [InlineData(@"%APPDATA%\Foo")]
    [InlineData(@"{Unknown}\Foo")]
    [InlineData(@"{Profile}\..\Foo")]
    [InlineData(@"{Profile}\.\Foo")]
    [InlineData(@"{Profile}\Foo\")]
    [InlineData(@"{Profile}\\Foo")]
    [InlineData(@"{Profile}\Cache*")]
    [InlineData(@"{Profile}\*Cache")]
    [InlineData(@"{Profile}\Foo\*\Ba*r")]
    [InlineData(@"{Profile}\Foo:bar")]
    [InlineData(@"{Profile}\{LocalAppData}\Foo")]
    [InlineData("{Profile}/Foo")]
    [InlineData("{profile}\\Foo")]
    public void TryParse_InvalidPattern_ReturnsAnError(string text)
    {
        Assert.False(PathPattern.TryParse(text, out PathPattern? pattern, out string? error));
        Assert.Null(pattern);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryParse_Null_ReturnsAnError()
    {
        Assert.False(PathPattern.TryParse(null, out _, out string? error));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(@"{Profile}\a\b\c", @"{Profile}\a\b", true)]
    [InlineData(@"{Profile}\a\b", @"{Profile}\a\b", true)]
    [InlineData(@"{Profile}\A\B", @"{Profile}\a\b", true)]
    [InlineData(@"{Profile}\a", @"{Profile}\a\b", false)]
    [InlineData(@"{Profile}\a\x", @"{Profile}\a\b", false)]
    [InlineData(@"{Profile}\a\*\c", @"{Profile}\a\b", true)]
    [InlineData(@"{Profile}\a\b\c", @"{Profile}\a\*", true)]
    public void MayLieInside_ComparesSegmentsIgnoringCase(string inner, string outer, bool expected)
    {
        Assert.Equal(expected, Parse(inner).MayLieInside(Parse(outer)));
    }
}

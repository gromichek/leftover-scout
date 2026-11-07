using LeftoverScout.Core.Help;

namespace LeftoverScout.Core.Tests.Help;

public class HelpLinkTests
{
    [Theory]
    [InlineData("scan.md", "scan")]
    [InlineData("./scan.md", "scan")]
    [InlineData("scan.md#buttons", "scan")]
    [InlineData("index.md", "overview")]
    [InlineData("getting-started.md", "getting-started")]
    public void Resolve_TopicFile_ReturnsTopic(string href, string expectedId)
    {
        HelpLink link = HelpLink.Resolve(href);

        Assert.Equal(HelpLinkKind.Topic, link.Kind);
        Assert.Equal(expectedId, link.Target);
    }

    [Fact]
    public void Resolve_Https_ReturnsExternal()
    {
        HelpLink link = HelpLink.Resolve("https://example.com/page");

        Assert.Equal(HelpLinkKind.External, link.Kind);
        Assert.Equal("https://example.com/page", link.Target);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://example.com")]
    [InlineData("file:///C:/x.md")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("missing.md")]
    [InlineData("../ru/scan.md")]
    [InlineData("image.png")]
    public void Resolve_Unsupported_IsIgnored(string? href)
    {
        Assert.Equal(HelpLinkKind.Ignored, HelpLink.Resolve(href).Kind);
    }
}

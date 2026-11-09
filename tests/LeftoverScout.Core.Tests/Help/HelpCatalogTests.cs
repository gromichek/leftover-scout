using LeftoverScout.Core.Help;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Core.Tests.Help;

public class HelpCatalogTests
{
    private sealed class FakeHelpSource : IHelpSource
    {
        private readonly Dictionary<(HelpLanguage, string), string> _files = [];

        public FakeHelpSource Add(HelpLanguage language, string fileName, string text)
        {
            _files[(language, fileName)] = text;
            return this;
        }

        public string? TryRead(HelpLanguage language, string fileName) =>
            _files.TryGetValue((language, fileName), out string? text) ? text : null;
    }

    private static HelpCatalog Create(FakeHelpSource source) => new(source, NullLogger<HelpCatalog>.Instance);

    [Fact]
    public void GetMarkdown_TopicExists_ReturnsTextOfRequestedLanguage()
    {
        var source = new FakeHelpSource()
            .Add(HelpLanguage.English, "scan.md", "# Scan")
            .Add(HelpLanguage.Russian, "scan.md", "# Scan (RU)");

        Assert.Equal("# Scan (RU)", Create(source).GetMarkdown(HelpTopics.Scan, HelpLanguage.Russian));
    }

    [Fact]
    public void GetMarkdown_MissingRussian_FallsBackToEnglish()
    {
        var source = new FakeHelpSource().Add(HelpLanguage.English, "scan.md", "# Scan");

        Assert.Equal("# Scan", Create(source).GetMarkdown(HelpTopics.Scan, HelpLanguage.Russian));
    }

    [Fact]
    public void GetMarkdown_MissingEverywhere_ReturnsNotFoundMessage()
    {
        string text = Create(new FakeHelpSource()).GetMarkdown(HelpTopics.Scan, HelpLanguage.English);

        Assert.Contains("not found", text);
    }

    [Fact]
    public void GetMarkdown_UnknownTopic_ReturnsNotFoundMessage()
    {
        string text = Create(new FakeHelpSource()).GetMarkdown("nope", HelpLanguage.English);

        Assert.Contains("not found", text);
    }

    [Fact]
    public void Topics_HaveUniqueIdsAndFileNames()
    {
        Assert.Equal(HelpTopics.All.Count, HelpTopics.All.Select(t => t.Id).Distinct().Count());
        Assert.Equal(HelpTopics.All.Count, HelpTopics.All.Select(t => t.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(
            ["overview", "getting-started", "scan", "results", "programs", "findings", "cleanup", "history", "about", "help"],
            HelpTopics.All.Select(t => t.Id));
    }
}

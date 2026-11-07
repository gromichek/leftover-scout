using LeftoverScout.App.Help;
using LeftoverScout.Core.Help;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LeftoverScout.App.Tests.Help;

public class HelpFilesTests
{
    private static readonly string[] Languages = ["en", "ru"];

    public static TheoryData<string, string> TopicsAndLanguages()
    {
        var data = new TheoryData<string, string>();
        foreach (HelpTopic topic in HelpTopics.All)
        {
            foreach (string language in Languages)
            {
                data.Add(topic.Id, language);
            }
        }

        return data;
    }

    public static TheoryData<string, string> AllFiles()
    {
        var data = new TheoryData<string, string>();
        foreach (string language in Languages)
        {
            foreach (string file in HelpFiles.Files(language))
            {
                data.Add(language, file);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(TopicsAndLanguages))]
    public void EveryTopic_IsEmbeddedInBothLanguages(string topicId, string language)
    {
        var source = new EmbeddedHelpSource();
        HelpTopic topic = HelpTopics.FindById(topicId)!;
        HelpLanguage helpLanguage = language == "ru" ? HelpLanguage.Russian : HelpLanguage.English;

        string? text = source.TryRead(helpLanguage, topic.FileName);

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void EveryTopicFile_IsListedInTopics_AndFilesMatchBetweenLanguages()
    {
        IReadOnlyList<string> english = HelpFiles.Files("en");
        IReadOnlyList<string> russian = HelpFiles.Files("ru");

        Assert.Equal(english, russian);
        Assert.Equal(
            HelpTopics.All.Select(t => t.FileName).Order(StringComparer.OrdinalIgnoreCase),
            english);
    }

    [Fact]
    public void EmbeddedFiles_MatchFilesInRepository()
    {
        var source = new EmbeddedHelpSource();
        var expected = Languages
            .SelectMany(language => HelpFiles.Files(language).Select(file => $"Help/{language}/{file}"))
            .Order(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(expected, source.GetFileNames().Order(StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(AllFiles))]
    public void EveryRelativeLink_PointsToExistingTopicFile(string language, string fileName)
    {
        MarkdownDocument document = Markdown.Parse(HelpFiles.ReadText(language, fileName), new MarkdownPipelineBuilder().UsePipeTables().Build());

        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            Assert.False(link.IsImage, $"{language}/{fileName}: images are not allowed.");
            string url = link.Url ?? string.Empty;
            HelpLink resolved = HelpLink.Resolve(url);
            if (resolved.Kind == HelpLinkKind.External)
            {
                continue;
            }

            Assert.True(resolved.Kind == HelpLinkKind.Topic, $"{language}/{fileName}: link '{url}' is not a topic link.");
            string target = url.Split('#')[0].Replace("./", string.Empty, StringComparison.Ordinal);
            Assert.True(
                File.Exists(System.IO.Path.Combine(HelpFiles.UserGuideFolder, language, target)),
                $"{language}/{fileName}: link '{url}' points to a missing file.");
        }
    }

    [Theory]
    [MemberData(nameof(AllFiles))]
    public void EveryFile_RendersWithoutException(string language, string fileName)
    {
        string text = HelpFiles.ReadText(language, fileName);

        int blocks = StaThread.Run(() => new HelpDocumentBuilder(_ => { }).Build(text).Blocks.Count);

        Assert.True(blocks > 0);
    }

    [Fact]
    public void RussianFiles_ContainCyrillic_AndEnglishFilesDoNot()
    {
        foreach (string file in HelpFiles.Files("en"))
        {
            Assert.False(HasCyrillic(HelpFiles.ReadText("en", file)), $"en/{file} contains Cyrillic text.");
        }

        foreach (string file in HelpFiles.Files("ru"))
        {
            Assert.True(HasCyrillic(HelpFiles.ReadText("ru", file)), $"ru/{file} has no Cyrillic text.");
        }
    }

    private static bool HasCyrillic(string text) => text.Any(c => c is >= '\u0400' and <= '\u04FF');
}

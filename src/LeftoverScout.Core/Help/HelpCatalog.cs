using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Help;

/// <summary>
/// Returns the Markdown text of a help topic in a language, falling back to English when a translation is missing.
/// </summary>
public sealed class HelpCatalog
{
    private readonly IHelpSource _source;
    private readonly ILogger _logger;

    /// <summary>Creates the catalog.</summary>
    public HelpCatalog(IHelpSource source, ILogger<HelpCatalog> logger)
    {
        _source = source;
        _logger = logger;
    }

    /// <summary>
    /// Returns the Markdown of a topic. A missing Russian topic falls back to English and logs a warning.
    /// An unknown id or a topic missing in every language gives a short plain message instead of throwing.
    /// </summary>
    /// <param name="topicId">Topic id, see <see cref="HelpTopics"/>.</param>
    /// <param name="language">Wanted language.</param>
    public string GetMarkdown(string topicId, HelpLanguage language)
    {
        HelpTopic? topic = HelpTopics.FindById(topicId);
        if (topic is null)
        {
            _logger.LogWarning("Unknown help topic {TopicId}.", topicId);
            return "# Help\n\nThis help topic was not found.";
        }

        string? text = _source.TryRead(language, topic.FileName);
        if (text is not null)
        {
            return text;
        }

        if (language != HelpLanguage.English)
        {
            _logger.LogWarning("Help topic {TopicId} is missing in {Language}, using English.", topic.Id, language);
            text = _source.TryRead(HelpLanguage.English, topic.FileName);
            if (text is not null)
            {
                return text;
            }
        }

        _logger.LogWarning("Help topic {TopicId} is missing.", topic.Id);
        return "# Help\n\nThis help topic was not found.";
    }
}

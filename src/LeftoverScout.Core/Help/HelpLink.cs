namespace LeftoverScout.Core.Help;

/// <summary>
/// The decision about one link of a help topic.
/// </summary>
/// <param name="Kind">What to do with the link.</param>
/// <param name="Target">The topic id for <see cref="HelpLinkKind.Topic"/>, the address for <see cref="HelpLinkKind.External"/>, otherwise empty.</param>
public sealed record HelpLink(HelpLinkKind Kind, string Target)
{
    private const string HttpsPrefix = "https://";

    /// <summary>
    /// Classifies a link as written in a Markdown file. A relative link to a topic file (<c>scan.md</c>, optionally with
    /// <c>./</c> or a <c>#section</c>) is a topic link; <c>https://</c> is external; everything else is ignored.
    /// </summary>
    public static HelpLink Resolve(string? href)
    {
        var ignored = new HelpLink(HelpLinkKind.Ignored, string.Empty);
        if (string.IsNullOrWhiteSpace(href))
        {
            return ignored;
        }

        string text = href.Trim();
        if (text.StartsWith(HttpsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new HelpLink(HelpLinkKind.External, text);
        }

        if (text.Contains(':', StringComparison.Ordinal))
        {
            return ignored;
        }

        int fragment = text.IndexOf('#', StringComparison.Ordinal);
        string file = fragment >= 0 ? text[..fragment] : text;
        if (file.StartsWith("./", StringComparison.Ordinal))
        {
            file = file[2..];
        }

        if (file.Contains('/', StringComparison.Ordinal) || file.Contains('\\', StringComparison.Ordinal))
        {
            return ignored;
        }

        HelpTopic? topic = HelpTopics.FindByFileName(file);
        return topic is null ? ignored : new HelpLink(HelpLinkKind.Topic, topic.Id);
    }
}

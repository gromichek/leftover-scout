namespace LeftoverScout.Core.Help;

/// <summary>
/// The list of help topics in the order shown in the Help page. Add a topic here and add its
/// Markdown file in every language folder under <c>docs/user-guide</c>.
/// </summary>
public static class HelpTopics
{
    /// <summary>Id of the overview topic.</summary>
    public const string Overview = "overview";

    /// <summary>Id of the topic about starting the app.</summary>
    public const string GettingStarted = "getting-started";

    /// <summary>Id of the Scan page topic.</summary>
    public const string Scan = "scan";

    /// <summary>Id of the Results page topic.</summary>
    public const string Results = "results";

    /// <summary>Id of the Cleanup page topic.</summary>
    public const string Cleanup = "cleanup";

    /// <summary>Id of the History page topic.</summary>
    public const string History = "history";

    /// <summary>Id of the About page topic.</summary>
    public const string About = "about";

    /// <summary>Id of the topic about the Help page itself.</summary>
    public const string Help = "help";

    /// <summary>All topics in display order.</summary>
    public static IReadOnlyList<HelpTopic> All { get; } =
    [
        new(Overview, "index.md", "Overview"),
        new(GettingStarted, "getting-started.md", "Getting started"),
        new(Scan, "scan.md", "Scan"),
        new(Results, "results.md", "Results"),
        new(Cleanup, "cleanup.md", "Cleanup"),
        new(History, "history.md", "History"),
        new(About, "about.md", "About"),
        new(Help, "help.md", "Help"),
    ];

    /// <summary>Finds a topic by id (ignoring case), or returns null.</summary>
    public static HelpTopic? FindById(string? id) =>
        All.FirstOrDefault(topic => string.Equals(topic.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>Finds a topic by file name (ignoring case), or returns null.</summary>
    public static HelpTopic? FindByFileName(string? fileName) =>
        All.FirstOrDefault(topic => string.Equals(topic.FileName, fileName, StringComparison.OrdinalIgnoreCase));
}

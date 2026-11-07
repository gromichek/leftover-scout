namespace LeftoverScout.Core.Help;

/// <summary>
/// Provides the Markdown text of help files. The app implements it with embedded resources.
/// </summary>
public interface IHelpSource
{
    /// <summary>Returns the text of the file, or null if it does not exist in that language.</summary>
    /// <param name="language">Language folder.</param>
    /// <param name="fileName">File name, for example <c>scan.md</c>.</param>
    string? TryRead(HelpLanguage language, string fileName);
}

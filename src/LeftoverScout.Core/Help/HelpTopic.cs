namespace LeftoverScout.Core.Help;

/// <summary>
/// One help topic: a stable id and the Markdown file name that is the same in every language.
/// </summary>
/// <param name="Id">Stable id, for example <c>scan</c>. Pages refer to topics by id.</param>
/// <param name="FileName">File name inside the language folder, for example <c>scan.md</c>.</param>
/// <param name="Title">Title shown in the topic list, English UI terms.</param>
public sealed record HelpTopic(string Id, string FileName, string Title);

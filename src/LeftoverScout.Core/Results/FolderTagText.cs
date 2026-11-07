using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// Converts the flags of a <see cref="FolderNode"/> into Notes tags and their texts.
/// </summary>
public static class FolderTagText
{
    private static readonly (FolderTags Tag, string Label, string Explanation)[] Definitions =
    [
        (FolderTags.Personal, "Personal", "Personal: one of your own folders (Documents, Desktop, Pictures, Videos, Music, Downloads or OneDrive). It is measured, but it will be protected from cleanup."),
        (FolderTags.Link, "Link", "Link: a junction or symbolic link. It is not followed and not counted, so nothing is counted twice."),
        (FolderTags.Cloud, "Cloud", "Cloud: an online-only OneDrive folder that is not available on this computer. It was not read, so that nothing is downloaded."),
        (FolderTags.Inaccessible, "Inaccessible", "Inaccessible: the folder could not be read completely (access denied or another error). Its size may be too small."),
    ];

    /// <summary>Returns the tags that apply to <paramref name="node"/>.</summary>
    public static FolderTags FromNode(FolderNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        FolderTags tags = FolderTags.None;
        if (node.IsPersonal)
        {
            tags |= FolderTags.Personal;
        }

        if (node.IsSkippedLink)
        {
            tags |= FolderTags.Link;
        }

        if (node.IsCloudFolderNotScanned)
        {
            tags |= FolderTags.Cloud;
        }

        if (node.IsAccessDenied || node.HasError)
        {
            tags |= FolderTags.Inaccessible;
        }

        return tags;
    }

    /// <summary>The tag names joined with ", " (empty when there is no tag).</summary>
    public static string Describe(FolderTags tags) =>
        string.Join(", ", Definitions.Where(definition => tags.HasFlag(definition.Tag)).Select(definition => definition.Label));

    /// <summary>One sentence per tag, one tag per line (empty when there is no tag). Used as tooltip.</summary>
    public static string Explain(FolderTags tags) =>
        string.Join("\n", Definitions.Where(definition => tags.HasFlag(definition.Tag)).Select(definition => definition.Explanation));
}

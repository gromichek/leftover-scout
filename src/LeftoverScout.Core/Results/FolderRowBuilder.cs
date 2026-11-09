using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// Builds the rows of the folder table for one folder (decisions D-031, D-032).
/// </summary>
public static class FolderRowBuilder
{
    /// <summary>Name of the extra row that stands for the files directly in the folder.</summary>
    public const string FilesRowName = "(files in this folder)";

    /// <summary>
    /// Builds the rows for the subfolders of <paramref name="folder"/>, plus the files row when the folder
    /// holds files directly. Rows are sorted by size, largest first, then by name.
    /// </summary>
    /// <param name="folder">The folder to show.</param>
    /// <param name="ruleMatches">The rules matched for the scan (decision D-043); null shows no rule labels.</param>
    public static IReadOnlyList<FolderRow> Build(FolderNode folder, RuleMatchMap? ruleMatches = null)
    {
        ArgumentNullException.ThrowIfNull(folder);

        var rows = new List<FolderRow>(folder.Children.Count + 1);
        long childSize = 0;
        long childFiles = 0;

        foreach (FolderNode child in folder.Children)
        {
            childSize += child.LocalSize;
            childFiles += child.FileCount;
            rows.Add(new FolderRow(
                child.Name,
                child.FullPath,
                child,
                IsFilesRow: false,
                child.LocalSize,
                child.FileCount,
                PercentOf(child.LocalSize, folder.LocalSize),
                child.NewestWriteTimeUtc,
                FolderTagText.FromNode(child),
                CanOpen(child))
            {
                RuleMatch = ruleMatches?.Get(child),
            });
        }

        long directFiles = Math.Max(0, folder.FileCount - childFiles);
        if (directFiles > 0)
        {
            long directSize = Math.Max(0, folder.LocalSize - childSize);
            rows.Add(new FolderRow(
                FilesRowName,
                folder.FullPath,
                Node: null,
                IsFilesRow: true,
                directSize,
                directFiles,
                PercentOf(directSize, folder.LocalSize),
                NewestWriteTimeUtc: null,
                FolderTags.None,
                CanOpen: false));
        }

        rows.Sort(CompareRows);
        return rows;
    }

    /// <summary>Share of <paramref name="part"/> in <paramref name="whole"/> in percent; 0 when the whole is 0.</summary>
    public static double PercentOf(long part, long whole) =>
        whole <= 0 ? 0 : Math.Clamp(part * 100.0 / whole, 0, 100);

    /// <summary>
    /// True when the folder can be opened in the table: links and cloud folders are never opened,
    /// and an unreadable folder only when it has children.
    /// </summary>
    public static bool CanOpen(FolderNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.IsSkippedLink || node.IsCloudFolderNotScanned)
        {
            return false;
        }

        return !(node.IsAccessDenied || node.HasError) || node.Children.Count > 0;
    }

    private static int CompareRows(FolderRow left, FolderRow right)
    {
        int bySize = right.LocalSize.CompareTo(left.LocalSize);
        return bySize != 0 ? bySize : StringComparer.OrdinalIgnoreCase.Compare(left.Name, right.Name);
    }
}

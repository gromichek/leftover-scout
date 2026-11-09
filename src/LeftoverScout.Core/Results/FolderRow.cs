using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// One row of the folder table: a subfolder, or the extra "(files in this folder)" row.
/// </summary>
/// <param name="Name">Folder name, or <see cref="FolderRowBuilder.FilesRowName"/>.</param>
/// <param name="FullPath">Full path; for the files row the path of the current folder.</param>
/// <param name="Node">The folder in the scan result; null for the files row.</param>
/// <param name="IsFilesRow">True for the "(files in this folder)" row.</param>
/// <param name="LocalSize">Local bytes in the subtree (for the files row: bytes of files directly in the folder).</param>
/// <param name="FileCount">Files in the subtree (for the files row: files directly in the folder).</param>
/// <param name="PercentOfParent">Share of the current folder's local size, 0 to 100; 0 when that size is 0.</param>
/// <param name="NewestWriteTimeUtc">Newest write time in the subtree; null when unknown.</param>
/// <param name="Tags">Notes tags.</param>
/// <param name="CanOpen">True when double-click or Enter may navigate into the row.</param>
public sealed record FolderRow(
    string Name,
    string FullPath,
    FolderNode? Node,
    bool IsFilesRow,
    long LocalSize,
    long FileCount,
    double PercentOfParent,
    DateTime? NewestWriteTimeUtc,
    FolderTags Tags,
    bool CanOpen)
{
    /// <summary>The rules that apply to the folder (ownership, cache, protected); null when none apply.</summary>
    public FolderRuleMatch? RuleMatch { get; init; }

    /// <summary>The leftover verdict (decision D-047); null when the folder was not evaluated.</summary>
    public LeftoverVerdict? Leftover { get; init; }
}

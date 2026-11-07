namespace LeftoverScout.Core.Scanning;

/// <summary>
/// A folder in the scan result. Totals include the whole subtree. Individual files are not kept (D-021).
/// </summary>
public sealed class FolderNode
{
    private readonly List<FolderNode> _children = [];

    internal FolderNode(string name, string fullPath, FolderNode? parent)
    {
        Name = name;
        FullPath = fullPath;
        Parent = parent;
    }

    /// <summary>Folder name (the full path for the root).</summary>
    public string Name { get; }

    /// <summary>Full path.</summary>
    public string FullPath { get; }

    /// <summary>Parent folder; null for the root.</summary>
    public FolderNode? Parent { get; }

    /// <summary>Subfolders sorted by name (ordinal, case-insensitive).</summary>
    public IReadOnlyList<FolderNode> Children => _children;

    /// <summary>Logical bytes of local files in the subtree.</summary>
    public long LocalSize { get; internal set; }

    /// <summary>Logical bytes of cloud-only files in the subtree.</summary>
    public long CloudOnlySize { get; internal set; }

    /// <summary>Number of files in the subtree (local and cloud-only).</summary>
    public long FileCount { get; internal set; }

    /// <summary>Number of subfolders in the subtree (not counting skipped links).</summary>
    public long SubfolderCount { get; internal set; }

    /// <summary>Newest file write time (UTC) in the subtree; null when there are no files.</summary>
    public DateTime? NewestWriteTimeUtc { get; internal set; }

    /// <summary>True for personal folders (D-020).</summary>
    public bool IsPersonal { get; internal set; }

    /// <summary>True for a junction or symbolic link that was not followed.</summary>
    public bool IsSkippedLink { get; internal set; }

    /// <summary>True for a cloud folder that is not populated locally and was not enumerated.</summary>
    public bool IsCloudFolderNotScanned { get; internal set; }

    /// <summary>True when the folder could not be read because access was denied.</summary>
    public bool IsAccessDenied { get; internal set; }

    /// <summary>True when the folder could not be read completely for a reason other than access denied.</summary>
    public bool HasError { get; internal set; }

    internal List<FolderNode> MutableChildren => _children;

    internal void AddFile(long length, DateTime lastWriteUtc, bool isCloudOnly)
    {
        if (isCloudOnly)
        {
            CloudOnlySize += length;
        }
        else
        {
            LocalSize += length;
        }

        FileCount++;
        MergeNewest(lastWriteUtc);
    }

    internal void MergeNewest(DateTime? candidate)
    {
        if (candidate is { } value && (NewestWriteTimeUtc is null || value > NewestWriteTimeUtc))
        {
            NewestWriteTimeUtc = value;
        }
    }

    internal void Absorb(FolderNode child)
    {
        LocalSize += child.LocalSize;
        CloudOnlySize += child.CloudOnlySize;
        FileCount += child.FileCount;
        SubfolderCount += child.SubfolderCount;
        MergeNewest(child.NewestWriteTimeUtc);
    }
}

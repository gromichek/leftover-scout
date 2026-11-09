using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// Keeps the folder that the Results page shows and moves between folders (decision D-031).
/// </summary>
public sealed class FolderNavigator
{
    /// <summary>Creates a navigator that starts at <paramref name="root"/>.</summary>
    public FolderNavigator(FolderNode root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Root = root;
        Current = root;
    }

    /// <summary>The profile root.</summary>
    public FolderNode Root { get; }

    /// <summary>The folder whose subfolders are shown.</summary>
    public FolderNode Current { get; private set; }

    /// <summary>True when there is a parent folder to go to.</summary>
    public bool CanGoUp => Current.Parent is not null;

    /// <summary>The path from the root to the current folder.</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumb
    {
        get
        {
            var nodes = new List<FolderNode>();
            for (FolderNode? node = Current; node is not null; node = node.Parent)
            {
                nodes.Add(node);
            }

            nodes.Reverse();
            return nodes
                .Select(node => new BreadcrumbItem(LabelOf(node), node, node.Parent is null, ReferenceEquals(node, Current)))
                .ToList();
        }
    }

    /// <summary>Opens the folder of <paramref name="row"/>. Returns false (and stays) when the row cannot be opened.</summary>
    public bool TryOpen(FolderRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!row.CanOpen || row.Node is null)
        {
            return false;
        }

        Current = row.Node;
        return true;
    }

    /// <summary>Goes to the parent folder. Returns false at the root.</summary>
    public bool GoUp()
    {
        if (Current.Parent is not { } parent)
        {
            return false;
        }

        Current = parent;
        return true;
    }

    /// <summary>Goes to a folder of the current breadcrumb. Returns false when the folder is not part of it.</summary>
    public bool NavigateTo(FolderNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        for (FolderNode? candidate = Current; candidate is not null; candidate = candidate.Parent)
        {
            if (ReferenceEquals(candidate, node))
            {
                Current = node;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Makes <paramref name="node"/> the current folder, wherever it is in the tree (used by "Show in Results").
    /// Returns false (and stays) when the folder does not belong to this tree.
    /// </summary>
    public bool GoTo(FolderNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        for (FolderNode? candidate = node; candidate is not null; candidate = candidate.Parent)
        {
            if (ReferenceEquals(candidate, Root))
            {
                Current = node;
                return true;
            }
        }

        return false;
    }

    private static string LabelOf(FolderNode node)
    {
        if (node.Parent is not null)
        {
            return node.Name;
        }

        // The root is named by its full path; the breadcrumb shows the profile folder name.
        string trimmed = node.Name.TrimEnd('\\', '/');
        int separator = trimmed.LastIndexOfAny(['\\', '/']);
        string last = separator >= 0 ? trimmed[(separator + 1)..] : trimmed;
        return last.Length > 0 ? last : node.Name;
    }
}

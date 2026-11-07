using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Results;

/// <summary>
/// One part of the breadcrumb path above the folder table.
/// </summary>
/// <param name="Label">Folder name; for the profile root its last path segment.</param>
/// <param name="Node">The folder this part leads to.</param>
/// <param name="IsRoot">True for the profile root.</param>
/// <param name="IsCurrent">True for the folder that is shown now.</param>
public sealed record BreadcrumbItem(string Label, FolderNode Node, bool IsRoot, bool IsCurrent)
{
    /// <summary>True when a separator is drawn before this part.</summary>
    public bool ShowSeparator => !IsRoot;
}

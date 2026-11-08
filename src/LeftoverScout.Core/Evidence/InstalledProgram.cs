namespace LeftoverScout.Core.Evidence;

/// <summary>
/// One piece of evidence that a program is (or was recently) installed.
/// </summary>
/// <param name="Name">Display name of the program, package, folder or shortcut.</param>
/// <param name="Source">Where the entry was found.</param>
/// <param name="SourceDetail">
/// Registry key path including hive and view, package family name, folder path, or the shortcut path
/// relative to the Start menu root, depending on <paramref name="Source"/>.
/// </param>
/// <param name="Publisher">Publisher, when known.</param>
/// <param name="Version">Version text, when known.</param>
/// <param name="InstallLocation">Folder where the program is installed, when known.</param>
/// <param name="InstallDate">Install date (date only), when known.</param>
/// <param name="IsSystemComponent">True for system components (registry SystemComponent = 1, system Store packages).</param>
/// <param name="IsFramework">True for framework packages (Store).</param>
public sealed record InstalledProgram(
    string Name,
    ProgramSource Source,
    string SourceDetail,
    string? Publisher = null,
    string? Version = null,
    string? InstallLocation = null,
    DateOnly? InstallDate = null,
    bool IsSystemComponent = false,
    bool IsFramework = false)
{
    /// <summary>For program folders: the names of the direct subfolders, kept for matching and not shown.</summary>
    public IReadOnlyList<string> SubfolderNames { get; init; } = [];
}

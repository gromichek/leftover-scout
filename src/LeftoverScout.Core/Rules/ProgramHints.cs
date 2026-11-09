namespace LeftoverScout.Core.Rules;

/// <summary>
/// The program a rule belongs to, with plain-text hints that a later task compares with the installed-program evidence.
/// All hint lists are matched case-insensitively and contain no wildcards.
/// </summary>
/// <param name="DisplayName">Name shown to the user.</param>
/// <param name="RequiresInstallation">
/// True when the program is normally installed (so missing evidence means it is gone); false for tools that often have no
/// installer entry, such as Gradle. Required for ownership rules; false when a cache rule leaves it out.
/// </param>
/// <param name="Names">Program or display names.</param>
/// <param name="Publishers">Publisher names.</param>
/// <param name="UninstallKeys">Names of the registry Uninstall keys.</param>
/// <param name="PackageFamilies">Store package family names.</param>
/// <param name="ProgramFolders">Names of the program's install folders.</param>
public sealed record ProgramHints(
    string DisplayName,
    bool RequiresInstallation,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Publishers,
    IReadOnlyList<string> UninstallKeys,
    IReadOnlyList<string> PackageFamilies,
    IReadOnlyList<string> ProgramFolders);

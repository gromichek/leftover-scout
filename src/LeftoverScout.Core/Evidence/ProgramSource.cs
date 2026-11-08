namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Where an <see cref="InstalledProgram"/> was found (decision D-035).
/// </summary>
public enum ProgramSource
{
    /// <summary>An entry of the Uninstall registry key (read-only).</summary>
    Registry,

    /// <summary>A Store / MSIX package of the current user.</summary>
    Store,

    /// <summary>A folder in Program Files or in the per-user Programs folder.</summary>
    ProgramFolder,

    /// <summary>A shortcut or folder in the Start menu.</summary>
    StartMenu,
}

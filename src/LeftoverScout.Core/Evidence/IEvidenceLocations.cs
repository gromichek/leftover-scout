namespace LeftoverScout.Core.Evidence;

/// <summary>
/// The folders that the folder-based evidence sources read. Implemented in <c>LeftoverScout.Windows</c>.
/// </summary>
public interface IEvidenceLocations
{
    /// <summary>Program Files, Program Files (x86) and the per-user Programs folder (only the ones that apply).</summary>
    IReadOnlyList<string> GetProgramFolderRoots();

    /// <summary>The user and all-users <c>Start Menu\Programs</c> folders.</summary>
    IReadOnlyList<string> GetStartMenuRoots();
}

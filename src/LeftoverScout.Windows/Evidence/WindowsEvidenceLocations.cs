using LeftoverScout.Core.Evidence;

namespace LeftoverScout.Windows.Evidence;

/// <summary>
/// The folders of the current user and computer that the folder-based evidence sources read.
/// </summary>
public sealed class WindowsEvidenceLocations : IEvidenceLocations
{
    /// <inheritdoc />
    public IReadOnlyList<string> GetProgramFolderRoots()
    {
        string localPrograms = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
        return NonEmpty(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            localPrograms);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetStartMenuRoots() =>
        NonEmpty(
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms));

    private static List<string> NonEmpty(params string[] paths) =>
        paths.Where(path => !string.IsNullOrWhiteSpace(path)).ToList();
}

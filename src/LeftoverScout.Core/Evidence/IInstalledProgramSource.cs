namespace LeftoverScout.Core.Evidence;

/// <summary>
/// One source of installed-program evidence. Implementations only read: they never write or change anything.
/// </summary>
public interface IInstalledProgramSource
{
    /// <summary>Which source this is.</summary>
    ProgramSource Source { get; }

    /// <summary>Reads the programs. May throw; the collector catches failures per source.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken);
}

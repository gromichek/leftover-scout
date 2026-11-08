using LeftoverScout.Core.Evidence;

namespace LeftoverScout.Core.Tests.Evidence;

public sealed class FakeEvidenceLocations(IReadOnlyList<string>? programFolders = null, IReadOnlyList<string>? startMenu = null) : IEvidenceLocations
{
    public IReadOnlyList<string> GetProgramFolderRoots() => programFolders ?? [];

    public IReadOnlyList<string> GetStartMenuRoots() => startMenu ?? [];
}

using LeftoverScout.Core.Evidence;
using LeftoverScout.Windows.Evidence;

namespace LeftoverScout.Windows.Tests.Evidence;

/// <summary>
/// Read-only smoke tests against the real machine. They assert almost nothing about the content (every
/// computer is different) and never write anything.
/// </summary>
public class WindowsEvidenceSmokeTests
{
    [Fact]
    public void UninstallRegistry_Hklm64_ReturnsAtLeastOneProgram()
    {
        var registry = new WindowsUninstallRegistry();
        var location = new UninstallLocation(UninstallHive.LocalMachine, UninstallView.Registry64);

        IReadOnlyList<UninstallEntry> entries = registry.ReadEntries(location, TestContext.Current.CancellationToken);
        IReadOnlyList<InstalledProgram> programs = UninstallEntryParser.ParseAll([(location, entries)]);

        Assert.NotEmpty(programs);
        Assert.All(programs, p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));
    }

    [Fact]
    public void RegistrySource_ReadsAllFourLocationsWithoutThrowing()
    {
        var source = new RegistryProgramSource(new WindowsUninstallRegistry());

        IReadOnlyList<InstalledProgram> programs = source.Collect(TestContext.Current.CancellationToken);

        Assert.NotEmpty(programs);
        Assert.All(programs, p => Assert.Equal(ProgramSource.Registry, p.Source));
    }

    [Fact]
    public void StorePackages_FindPackagesForUser_ReturnsWithoutThrowing()
    {
        var source = new StoreProgramSource(new WinRtStorePackageProvider());

        // The number of packages depends on the computer, so it is not asserted.
        IReadOnlyList<InstalledProgram> programs = source.Collect(TestContext.Current.CancellationToken);

        Assert.All(programs, p => Assert.Equal(ProgramSource.Store, p.Source));
    }

    [Fact]
    public void Locations_ReturnAbsolutePathsOnly()
    {
        var locations = new WindowsEvidenceLocations();

        Assert.All(locations.GetProgramFolderRoots().Concat(locations.GetStartMenuRoots()), path => Assert.True(System.IO.Path.IsPathFullyQualified(path)));
        Assert.NotEmpty(locations.GetStartMenuRoots());
    }
}

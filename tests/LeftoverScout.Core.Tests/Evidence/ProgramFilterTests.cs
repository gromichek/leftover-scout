using LeftoverScout.Core.Evidence;

namespace LeftoverScout.Core.Tests.Evidence;

public class ProgramFilterTests
{
    private static readonly InstalledProgram[] Programs =
    [
        new("Zeta Editor", ProgramSource.Registry, "k1", Publisher: "Sample Publisher"),
        new("alpha Tool", ProgramSource.Store, "k2", Publisher: "Other Company"),
        new("Beta Runtime", ProgramSource.Registry, "k3", IsSystemComponent: true),
        new("Gamma Framework", ProgramSource.Store, "k4", IsFramework: true),
        new("Sample Suite", ProgramSource.ProgramFolder, "k5"),
        new("Sample Suite", ProgramSource.StartMenu, "k6"),
    ];

    [Fact]
    public void Apply_NoFilters_HidesSystemAndFrameworkAndSortsByName()
    {
        IReadOnlyList<InstalledProgram> result = ProgramFilter.Apply(Programs, null, null, includeSystemComponents: false);

        Assert.Equal(["alpha Tool", "Sample Suite", "Sample Suite", "Zeta Editor"], result.Select(p => p.Name));
        Assert.Equal(ProgramSource.ProgramFolder, result[1].Source);
    }

    [Fact]
    public void Apply_ShowSystem_IncludesSystemComponentsAndFrameworks()
    {
        IReadOnlyList<InstalledProgram> result = ProgramFilter.Apply(Programs, string.Empty, null, includeSystemComponents: true);

        Assert.Equal(6, result.Count);
    }

    [Theory]
    [InlineData("ZETA", "Zeta Editor")]
    [InlineData("  zeta  ", "Zeta Editor")]
    [InlineData("other comp", "alpha Tool")]
    public void Apply_Search_MatchesNameOrPublisherIgnoringCase(string search, string expected)
    {
        IReadOnlyList<InstalledProgram> result = ProgramFilter.Apply(Programs, search, null, includeSystemComponents: false);

        Assert.Equal(expected, Assert.Single(result).Name);
    }

    [Fact]
    public void Apply_SearchWithoutMatch_ReturnsEmpty()
    {
        Assert.Empty(ProgramFilter.Apply(Programs, "nothing like this", null, includeSystemComponents: true));
    }

    [Fact]
    public void Apply_SourceFilter_KeepsOnlyThatSource()
    {
        IReadOnlyList<InstalledProgram> result = ProgramFilter.Apply(Programs, null, ProgramSource.Store, includeSystemComponents: true);

        Assert.Equal(["alpha Tool", "Gamma Framework"], result.Select(p => p.Name));
    }

    [Fact]
    public void Apply_SourceAndSystemFilter_Combine()
    {
        IReadOnlyList<InstalledProgram> result = ProgramFilter.Apply(Programs, null, ProgramSource.Store, includeSystemComponents: false);

        Assert.Equal(["alpha Tool"], result.Select(p => p.Name));
    }

    [Theory]
    [InlineData(ProgramSource.Registry, "Registry")]
    [InlineData(ProgramSource.Store, "Store")]
    [InlineData(ProgramSource.ProgramFolder, "Program folder")]
    [InlineData(ProgramSource.StartMenu, "Start menu")]
    public void SourceText_ReturnsUiText(ProgramSource source, string expected)
    {
        Assert.Equal(expected, ProgramFilter.SourceText(source));
    }
}

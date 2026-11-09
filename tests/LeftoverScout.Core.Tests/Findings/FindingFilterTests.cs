using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Classification;
using static LeftoverScout.Core.Tests.Findings.FindingTestKit;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;

namespace LeftoverScout.Core.Tests.Findings;

public class FindingFilterTests
{
    private static readonly DateTime Recent = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static IReadOnlyList<Finding> SampleFindings()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\FooSoft", "a.dat", Old, 100)
            .File(@"AppData\Roaming\FooSoft\Cache", "b.bin", Old, 40)
            .File(@"AppData\Roaming\BarTool", "c.dat", Recent, 7)
            .File(@"AppData\Local\Temp", "d.tmp", Old, 30)
            .File(@"AppData\Local\SampleLogs", "e.log", Old, 20)
            .File(@"AppData\Local\SampleCrashes", "f.dmp", Old, 10)
            .Build();

        return FindingBuilder.Build(Result(
            root,
            Cache("cache", @"{RoamingAppData}\*\Cache", "cache"),
            Cache("temp", @"{LocalAppData}\Temp", "temp"),
            Cache("logs", @"{LocalAppData}\SampleLogs", "logs"),
            Cache("dumps", @"{LocalAppData}\SampleCrashes", "crashDumps")));
    }

    private static IEnumerable<string> Paths(FindingList list) => list.Entries.Select(entry => entry.Finding.RelativePath);

    [Fact]
    public void Default_HidesLowAndShowsEverythingElse()
    {
        FindingFilter filter = FindingFilter.Default;

        Assert.True(filter.ShowHigh);
        Assert.True(filter.ShowMedium);
        Assert.False(filter.ShowLow);
        Assert.True(filter.ShowCache && filter.ShowTemp && filter.ShowLogs && filter.ShowCrashDumps);
        Assert.Equal(string.Empty, filter.SearchText);
    }

    [Fact]
    public void Apply_Default_ListsAllButLow()
    {
        FindingList list = FindingFilter.Default.Apply(SampleFindings());

        Assert.Equal(5, list.Count);
        Assert.DoesNotContain(@"AppData\Roaming\BarTool", Paths(list));
    }

    [Fact]
    public void Apply_ShowLow_AddsLowLeftovers()
    {
        FindingList list = (FindingFilter.Default with { ShowLow = true }).Apply(SampleFindings());

        Assert.Equal(6, list.Count);
        Assert.Contains(@"AppData\Roaming\BarTool", Paths(list));
    }

    [Theory]
    [InlineData("Cache")]
    [InlineData("Temp")]
    [InlineData("Logs")]
    [InlineData("CrashDumps")]
    public void Apply_HidingOneCacheKind_RemovesOnlyThatKind(string hidden)
    {
        FindingFilter filter = hidden switch
        {
            "Cache" => FindingFilter.Default with { ShowCache = false },
            "Temp" => FindingFilter.Default with { ShowTemp = false },
            "Logs" => FindingFilter.Default with { ShowLogs = false },
            _ => FindingFilter.Default with { ShowCrashDumps = false },
        };

        FindingList list = filter.Apply(SampleFindings());

        Assert.Equal(4, list.Count);
        Assert.DoesNotContain(list.Entries, entry => entry.Finding.Kind.ToString() == hidden);
    }

    [Fact]
    public void Apply_HidingMedium_RemovesMediumLeftovers()
    {
        FindingList list = (FindingFilter.Default with { ShowMedium = false }).Apply(SampleFindings());

        Assert.DoesNotContain(@"AppData\Roaming\FooSoft", Paths(list));
    }

    [Fact]
    public void Apply_Search_MatchesPathCaseInsensitively()
    {
        FindingList list = (FindingFilter.Default with { SearchText = "  fOOsoft " }).Apply(SampleFindings());

        Assert.Equal([@"AppData\Roaming\FooSoft", @"AppData\Roaming\FooSoft\Cache"], Paths(list).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Apply_Search_MatchesProgramName()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\FooSoft", "a.dat", Old, 10).Build();
        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root, [Owner("o", @"{RoamingAppData}\FooSoft", "Fancy Editor")], (@"AppData\Roaming\FooSoft", Leftover())));

        Assert.Single((FindingFilter.Default with { SearchText = "fancy" }).Apply(findings).Entries);
        Assert.Empty((FindingFilter.Default with { SearchText = "other" }).Apply(findings).Entries);
    }

    [Fact]
    public void Apply_NestedVisibleItem_IsShownButNotCountedTwice()
    {
        FindingList list = FindingFilter.Default.Apply(SampleFindings());

        FindingEntry nested = list.Entries.Single(entry => entry.Finding.RelativePath == @"AppData\Roaming\FooSoft\Cache");
        Assert.Equal(@"AppData\Roaming\FooSoft", nested.InsideOf!.RelativePath);
        Assert.Equal(@"Inside another finding: AppData\Roaming\FooSoft", nested.InsideNote);
        // FooSoft 140 (includes Cache), Temp 30, Logs 20, Crashes 10; the nested Cache (40) is not added again.
        Assert.Equal(140 + 30 + 20 + 10, list.TotalSize);
    }

    [Fact]
    public void Apply_OuterItemFilteredOut_NestedItemIsCounted()
    {
        FindingList list = (FindingFilter.Default with { ShowMedium = false }).Apply(SampleFindings());

        FindingEntry cache = list.Entries.Single(entry => entry.Finding.RelativePath == @"AppData\Roaming\FooSoft\Cache");
        Assert.Null(cache.InsideOf);
        Assert.Equal(string.Empty, cache.InsideNote);
        Assert.Equal(40 + 30 + 20 + 10, list.TotalSize);
    }

    [Fact]
    public void Apply_NoFindings_IsEmpty()
    {
        FindingList list = FindingFilter.Default.Apply([]);

        Assert.Equal(0, list.Count);
        Assert.Equal(0, list.TotalSize);
    }
}

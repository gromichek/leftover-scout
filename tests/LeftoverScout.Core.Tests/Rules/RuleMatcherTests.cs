using System.Diagnostics;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Rules;

public class RuleMatcherTests
{
    private static RuleMatcher Matcher(params string[] ruleJson)
    {
        RuleLoadResult result = Load(ruleJson);
        Assert.Empty(result.Errors);
        return new RuleMatcher(result.RuleSet);
    }

    private static string[] Ids(RuleMatchMap map, FolderNode root, string relativePath) =>
        map.Get(Find(root, relativePath))?.All.Select(match => match.Rule.Id).ToArray() ?? [];

    [Fact]
    public void Match_TokenAndLiteralSegments_MatchesTheFolder()
    {
        FolderNode root = Tree(null, @"AppData\Local\Temp", @"AppData\Local\Other");
        RuleMatchMap map = Matcher(Cache("user-temp", @"{LocalAppData}\Temp", "temp")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["user-temp"], Ids(map, root, @"AppData\Local\Temp"));
        Assert.Empty(Ids(map, root, @"AppData\Local\Other"));
    }

    [Fact]
    public void Match_IsCaseInsensitive()
    {
        FolderNode root = Tree(null, @"appdata\LOCAL\temp");
        RuleMatchMap map = Matcher(Cache("user-temp", @"{LocalAppData}\Temp", "temp", "folder")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["user-temp"], Ids(map, root, @"AppData\Local\Temp"));
    }

    [Fact]
    public void Match_SegmentPrefix_DoesNotMatch()
    {
        FolderNode root = Tree(null, @"AppData\Local\App\CacheStorage", @"AppData\Local\App\Cache2", @"AppData\Local\App\Cache");
        RuleMatchMap map = Matcher(Cache("app-cache", @"{LocalAppData}\App\Cache", "cache", "folder")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["app-cache"], Ids(map, root, @"AppData\Local\App\Cache"));
        Assert.Empty(Ids(map, root, @"AppData\Local\App\CacheStorage"));
        Assert.Empty(Ids(map, root, @"AppData\Local\App\Cache2"));
    }

    [Fact]
    public void Match_Wildcard_MatchesExactlyOneFolderName()
    {
        FolderNode root = Tree(null, @"AppData\Local\Browser\Default\Cache", @"AppData\Local\Browser\Profile 1\Cache", @"AppData\Local\Browser\Cache", @"AppData\Local\Browser\Default\Deeper\Cache");
        RuleMatchMap map = Matcher(Cache("browser-cache", @"{LocalAppData}\Browser\*\Cache", "cache", "folder")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["browser-cache"], Ids(map, root, @"AppData\Local\Browser\Default\Cache"));
        Assert.Equal(["browser-cache"], Ids(map, root, @"AppData\Local\Browser\Profile 1\Cache"));
        Assert.Empty(Ids(map, root, @"AppData\Local\Browser\Cache"));
        Assert.Empty(Ids(map, root, @"AppData\Local\Browser\Default\Deeper\Cache"));
    }

    [Fact]
    public void Match_ProfileToken_UsesTheRootOfTheScan()
    {
        FolderNode root = Tree(null, @".nuget\packages", @"Other\.nuget\packages");
        RuleMatchMap map = Matcher(Cache("nuget", @"{Profile}\.nuget\packages")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["nuget"], Ids(map, root, @".nuget\packages"));
        Assert.Empty(Ids(map, root, @"Other\.nuget\packages"));
    }

    [Fact]
    public void Match_ProfileTokenAlone_MatchesTheRoot()
    {
        FolderNode root = Tree(null, "Sub");
        RuleMatchMap map = Matcher(Protected("protect-root", "{Profile}", "folder")).Match(root, TestContext.Current.CancellationToken);

        Assert.True(map.Get(root)!.IsProtected);
        Assert.Null(map.Get(Find(root, "Sub")));
    }

    [Fact]
    public void Match_FolderScope_AppliesOnlyToTheFolder()
    {
        FolderNode root = Tree(null, @"AppData\Local\Child");
        RuleMatchMap map = Matcher(Protected("protect-appdata", @"{Profile}\AppData", "folder")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["protect-appdata"], Ids(map, root, "AppData"));
        Assert.Empty(Ids(map, root, @"AppData\Local"));
        Assert.Empty(Ids(map, root, @"AppData\Local\Child"));
    }

    [Fact]
    public void Match_SubtreeScope_AppliesToDescendantsAsInheritedMatches()
    {
        FolderNode root = Tree(null, @"AppData\Roaming\Microsoft\Protect\Keys\Deep", @"AppData\Roaming\Microsoft\Other");
        RuleMatchMap map = Matcher(Protected("protect-keys", @"{RoamingAppData}\Microsoft\Protect", "subtree")).Match(root, TestContext.Current.CancellationToken);

        FolderRuleMatch direct = map.Get(Find(root, @"AppData\Roaming\Microsoft\Protect"))!;
        FolderRuleMatch deep = map.Get(Find(root, @"AppData\Roaming\Microsoft\Protect\Keys\Deep"))!;
        Assert.False(direct.Primary.IsInherited);
        Assert.True(deep.IsProtected);
        Assert.True(deep.Primary.IsInherited);
        Assert.Equal("protect-keys", deep.Primary.Rule.Id);
        Assert.Empty(Ids(map, root, @"AppData\Roaming\Microsoft\Other"));
    }

    [Fact]
    public void Match_PersonalFolder_IsProtectedWithItsSubtree()
    {
        FolderNode root = Tree([Root + @"\Documents"], @"Documents\Projects\Deep", "Other");
        RuleMatchMap map = Matcher().Match(root, TestContext.Current.CancellationToken);

        FolderRuleMatch documents = map.Get(Find(root, "Documents"))!;
        FolderRuleMatch deep = map.Get(Find(root, @"Documents\Projects\Deep"))!;
        Assert.True(documents.IsProtected);
        Assert.True(documents.Primary.IsPersonalFolder);
        Assert.Equal("personal folder", documents.Primary.Rule.Source);
        Assert.True(deep.IsProtected);
        Assert.Null(map.Get(Find(root, "Other")));
    }

    [Fact]
    public void Match_PersonalFolderBeatsCacheRules()
    {
        FolderNode root = Tree([Root + @"\Documents"], @"Documents\Temp");
        RuleMatchMap map = Matcher(Cache("any-temp", @"{Profile}\Documents\Temp", "temp")).Match(root, TestContext.Current.CancellationToken);

        FolderRuleMatch temp = map.Get(Find(root, @"Documents\Temp"))!;
        Assert.True(temp.IsProtected);
        Assert.Contains(temp.All, match => match.Rule.Id == "any-temp");
    }

    [Fact]
    public void Match_ProtectedWinsOverMoreSpecificMatches()
    {
        // The loader would reject this combination; the matcher must still be safe if a rule set is built by hand.
        var protectedRule = new Rule("protect-a", RuleKind.Protected, [Pattern(@"{Profile}\a")], RuleScope.Folder, null, null, "d", "s");
        var cacheRule = new Rule("cache-a", RuleKind.Cache, [Pattern(@"{Profile}\a")], RuleScope.Folder, CacheCategory.Cache, null, "d", "s");
        FolderNode root = Tree(null, "a");

        RuleMatchMap map = new RuleMatcher(new RuleSet([cacheRule, protectedRule])).Match(root, TestContext.Current.CancellationToken);

        FolderRuleMatch match = map.Get(Find(root, "a"))!;
        Assert.True(match.IsProtected);
        Assert.Equal("protect-a", match.Primary.Rule.Id);
        Assert.Equal(2, match.All.Count);
    }

    [Fact]
    public void Match_MoreLiteralSegments_IsThePrimaryMatch()
    {
        FolderNode root = Tree(null, @"AppData\Roaming\Sample\Cache");
        RuleMatchMap map = Matcher(
            Owner("sample-owner", @"{RoamingAppData}\Sample"),
            Cache("sample-cache", @"{RoamingAppData}\*\Cache"),
            Cache("sample-cache-exact", @"{RoamingAppData}\Sample\Cache", "cache", "folder")).Match(root, TestContext.Current.CancellationToken);

        FolderRuleMatch match = map.Get(Find(root, @"AppData\Roaming\Sample\Cache"))!;
        Assert.Equal(["sample-cache-exact", "sample-cache", "sample-owner"], match.All.Select(item => item.Rule.Id));
        Assert.Equal("sample-cache-exact", match.Primary.Rule.Id);
    }

    [Fact]
    public void Match_DirectMatchBeatsInheritedMatch()
    {
        FolderNode root = Tree(null, @"AppData\Roaming\Sample\Cache\Sub");
        RuleMatchMap map = Matcher(
            Owner("sample-owner", @"{RoamingAppData}\Sample"),
            Cache("sample-cache", @"{RoamingAppData}\Sample\Cache")).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["sample-cache", "sample-owner"], Ids(map, root, @"AppData\Roaming\Sample\Cache"));
        Assert.Equal(["sample-cache", "sample-owner"], Ids(map, root, @"AppData\Roaming\Sample\Cache\Sub"));
        Assert.Equal(["sample-owner"], Ids(map, root, @"AppData\Roaming\Sample"));
    }

    [Fact]
    public void Match_SeveralPatternsOfOneRule_AllMatch()
    {
        FolderNode root = Tree(null, @"AppData\Local\One", @"AppData\Local\Two");
        var rule = Load("""{ "id": "two-paths", "kind": "cache", "paths": ["{LocalAppData}\\One", "{LocalAppData}\\Two"], "category": "cache", "scope": "folder", "description": "d", "source": "s" }""");

        RuleMatchMap map = new RuleMatcher(rule.RuleSet).Match(root, TestContext.Current.CancellationToken);

        Assert.Equal(["two-paths"], Ids(map, root, @"AppData\Local\One"));
        Assert.Equal(["two-paths"], Ids(map, root, @"AppData\Local\Two"));
    }

    [Fact]
    public void Match_NoRulesAndNoPersonalFolders_ReturnsAnEmptyMap()
    {
        FolderNode root = Tree(null, @"AppData\Local\Temp");

        Assert.Equal(0, new RuleMatcher(RuleSet.Empty).Match(root, TestContext.Current.CancellationToken).Count);
    }

    [Fact]
    public void Match_Cancelled_Throws()
    {
        FolderNode root = Tree(null, "a");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => new RuleMatcher(RuleSet.Empty).Match(root, cts.Token));
    }

    [Fact]
    public void Match_SyntheticTreeWith100000Folders_FinishesQuickly()
    {
        const int Top = 1000;
        const int Sub = 100;
        var reader = new FakeDirectoryReader();
        var topEntries = new List<DirectoryEntry>();
        for (int i = 0; i < Top; i++)
        {
            string name = "Top" + i;
            topEntries.Add(DirEntry(name));
            reader.Folder(Root + "\\" + name, [.. Enumerable.Range(0, Sub).Select(j => DirEntry("Sub" + j))]);
        }

        reader.Folder(Root, [.. topEntries, DirEntry("AppData")]);
        reader.Folder(Root + @"\AppData", DirEntry("Local"));
        reader.Folder(Root + @"\AppData\Local", DirEntry("Temp"));
        FolderNode root = Scan(reader);
        Assert.True(root.SubfolderCount >= 100_000);

        RuleMatcher matcher = Matcher(
            Cache("user-temp", @"{LocalAppData}\Temp", "temp"),
            Cache("wild-cache", @"{Profile}\*\Sub5\Cache"),
            Owner("top-owner", @"{Profile}\Top7"));

        var watch = Stopwatch.StartNew();
        RuleMatchMap map = matcher.Match(root, TestContext.Current.CancellationToken);
        watch.Stop();

        Assert.Equal(["user-temp"], Ids(map, root, @"AppData\Local\Temp"));
        Assert.Equal(1 + Sub + 1, map.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"Matching took {watch.Elapsed}.");
    }

    private static PathPattern Pattern(string text)
    {
        Assert.True(PathPattern.TryParse(text, out PathPattern? pattern, out string? error), error);
        return pattern;
    }
}

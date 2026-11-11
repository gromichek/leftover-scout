using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;

namespace LeftoverScout.Core.Tests.Rules;

public class RuleMatcherPathTests
{
    private static RuleMatcher Matcher(params string[] ruleJson)
    {
        RuleLoadResult result = Load(ruleJson);
        Assert.Empty(result.Errors);
        return new RuleMatcher(result.RuleSet);
    }

    [Fact]
    public void MatchPath_NoRule_ReturnsNull()
    {
        Assert.Null(Matcher(Cache("temp", @"{LocalAppData}\Temp")).MatchPath(["AppData", "Local", "Other"]));
    }

    [Fact]
    public void MatchPath_DirectRule_IsFoundIgnoringCase()
    {
        FolderRuleMatch? match = Matcher(Cache("temp", @"{LocalAppData}\Temp", "temp", "folder")).MatchPath(["appdata", "LOCAL", "temp"]);

        Assert.NotNull(match);
        Assert.Equal(["temp"], match.All.Select(m => m.Rule.Id).ToArray());
        Assert.False(match.Primary.IsInherited);
    }

    [Fact]
    public void MatchPath_SubtreeRuleOfAnAncestor_IsInherited()
    {
        FolderRuleMatch? match = Matcher(Protected("keep", @"{Profile}\Secret", "subtree")).MatchPath(["Secret", "a", "b"]);

        Assert.NotNull(match);
        Assert.True(match.IsProtected);
        Assert.True(match.Primary.IsInherited);
    }

    [Fact]
    public void MatchPath_FolderScopeRuleOfAnAncestor_IsNotInherited()
    {
        Assert.Null(Matcher(Protected("keep", @"{Profile}\Secret", "folder")).MatchPath(["Secret", "a"]));
    }

    [Fact]
    public void MatchPath_ProtectedWinsOverAnotherMatch()
    {
        FolderRuleMatch? match = Matcher(
            Protected("keep", @"{Profile}\Apps\*", "folder"),
            Cache("cache", @"{Profile}\Apps\One", "cache", "folder")).MatchPath(["Apps", "One"]);

        Assert.NotNull(match);
        Assert.Equal("keep", match.Primary.Rule.Id);
    }

    [Fact]
    public void ContainsProtectedPath_ParentOfAProtectedFolder_IsTrue()
    {
        RuleMatcher matcher = Matcher(Protected("keys", @"{Profile}\Apps\*\Keys", "subtree"));

        Assert.True(matcher.ContainsProtectedPath([]));
        Assert.True(matcher.ContainsProtectedPath(["Apps"]));
        Assert.True(matcher.ContainsProtectedPath(["apps", "One"]));
        Assert.False(matcher.ContainsProtectedPath(["Apps", "One", "Keys"]));
        Assert.False(matcher.ContainsProtectedPath(["Apps", "One", "Other"]));
        Assert.False(matcher.ContainsProtectedPath(["Other"]));
    }

    [Fact]
    public void ContainsProtectedPath_OnlyCacheRules_IsFalse()
    {
        Assert.False(Matcher(Cache("temp", @"{LocalAppData}\Temp")).ContainsProtectedPath(["AppData"]));
    }
}

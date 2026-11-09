using LeftoverScout.Core.Results;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Results;

public class FolderTagTextTests
{
    [Theory]
    [InlineData(FolderTags.None, "")]
    [InlineData(FolderTags.Personal, "Personal")]
    [InlineData(FolderTags.Link, "Link")]
    [InlineData(FolderTags.Cloud, "Cloud")]
    [InlineData(FolderTags.Inaccessible, "Inaccessible")]
    [InlineData(FolderTags.Personal | FolderTags.Inaccessible, "Personal, Inaccessible")]
    [InlineData(FolderTags.Personal | FolderTags.Link | FolderTags.Cloud | FolderTags.Inaccessible, "Personal, Link, Cloud, Inaccessible")]
    public void Describe_Tags_JoinsNamesInFixedOrder(FolderTags tags, string expected)
    {
        Assert.Equal(expected, FolderTagText.Describe(tags));
    }

    [Fact]
    public void Describe_EveryFlagCombination_NamesMatchTheFlags()
    {
        for (int bits = 0; bits < 16; bits++)
        {
            var tags = (FolderTags)bits;
            string text = FolderTagText.Describe(tags);
            string[] names = text.Length == 0 ? [] : text.Split(", ");

            Assert.Equal(int.PopCount(bits), names.Length);
            Assert.Equal(tags.HasFlag(FolderTags.Personal), names.Contains("Personal"));
            Assert.Equal(tags.HasFlag(FolderTags.Link), names.Contains("Link"));
            Assert.Equal(tags.HasFlag(FolderTags.Cloud), names.Contains("Cloud"));
            Assert.Equal(tags.HasFlag(FolderTags.Inaccessible), names.Contains("Inaccessible"));
        }
    }

    [Fact]
    public void Explain_Tags_HasOneLinePerTag()
    {
        Assert.Equal(string.Empty, FolderTagText.Explain(FolderTags.None));
        Assert.Single(FolderTagText.Explain(FolderTags.Link).Split('\n'));
        Assert.Equal(4, FolderTagText.Explain(FolderTags.Personal | FolderTags.Link | FolderTags.Cloud | FolderTags.Inaccessible).Split('\n').Length);
        Assert.StartsWith("Personal:", FolderTagText.Explain(FolderTags.Personal));
    }

    [Fact]
    public void FromNode_AccessDeniedAndOtherError_BothMeanInaccessible()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Denied"), DirEntry("Broken"))
            .Failing(Root + @"\Denied", "Access denied", accessDenied: true)
            .Failing(Root + @"\Broken", "Some error", accessDenied: false);
        ScanResult result = ResultsTestTree.Scan(reader);

        Assert.All(result.Root!.Children, node => Assert.Equal(FolderTags.Inaccessible, FolderTagText.FromNode(node)));
    }

    private static FolderRuleMatch MatchFor(string ruleJson, string relativePath, params string[] extraRuleJson)
    {
        RuleLoadResult loaded = RuleLoader.Load([new RuleFile("test.json", $$"""{ "schemaVersion": 1, "rules": [ {{string.Join(",", extraRuleJson.Prepend(ruleJson))}} ] }""")]);
        Assert.Empty(loaded.Errors);
        FolderNode root = Rules.RuleTestHelper.Tree(null, relativePath);
        return new RuleMatcher(loaded.RuleSet).Match(root, TestContext.Current.CancellationToken).Get(Rules.RuleTestHelper.Find(root, relativePath))!;
    }

    private static string CacheRule(string id, string path, string category, string? program = null) =>
        $$"""{ "id": "{{id}}", "kind": "cache", "paths": ["{{path.Replace("\\", "\\\\")}}"], "category": "{{category}}", "scope": "folder", {{(program is null ? "" : $"\"program\": {{ \"displayName\": \"{program}\" }},")}} "description": "Rule {{id}}.", "source": "s" }""";

    [Theory]
    [InlineData("cache", "Sample App", "Cache: Sample App")]
    [InlineData("cache", null, "Cache")]
    [InlineData("temp", "Sample App", "Temp")]
    [InlineData("logs", null, "Logs")]
    [InlineData("crashDumps", null, "Crash dumps")]
    public void RuleLabel_CacheRule_UsesProgramOrCategory(string category, string? program, string expected)
    {
        FolderRuleMatch match = MatchFor(CacheRule("sample-rule", @"{Profile}\Sample", category, program), "Sample");

        Assert.Equal(expected, FolderTagText.RuleLabel(match));
    }

    [Fact]
    public void RuleLabel_OwnershipRule_ShowsOwner()
    {
        FolderRuleMatch match = MatchFor(
            """{ "id": "sample-owner", "kind": "ownership", "paths": ["{Profile}\\Sample"], "program": { "displayName": "Sample App", "requiresInstallation": true }, "description": "d", "source": "s" }""",
            "Sample");

        Assert.Equal("Owner: Sample App", FolderTagText.RuleLabel(match));
    }

    [Fact]
    public void RuleLabel_ProtectedRule_ShowsProtected()
    {
        FolderRuleMatch match = MatchFor(
            """{ "id": "sample-protected", "kind": "protected", "paths": ["{Profile}\\Sample"], "scope": "folder", "description": "d", "source": "s" }""",
            "Sample");

        Assert.Equal("Protected", FolderTagText.RuleLabel(match));
    }

    [Fact]
    public void RuleLabel_NoMatch_IsEmpty()
    {
        Assert.Equal(string.Empty, FolderTagText.RuleLabel(null));
    }

    [Fact]
    public void RuleLabel_PersonalFolder_IsEmptyBecauseThePersonalTagIsShown()
    {
        FolderNode root = Rules.RuleTestHelper.Tree([Root + @"\Documents"], "Documents");
        FolderRuleMatch match = new RuleMatcher(RuleSet.Empty).Match(root, TestContext.Current.CancellationToken).Get(Rules.RuleTestHelper.Find(root, "Documents"))!;

        Assert.Equal(string.Empty, FolderTagText.RuleLabel(match));
        Assert.Equal("Personal", FolderTagText.Describe(FolderTags.Personal, match));
        Assert.Contains("never offered", FolderTagText.Explain(FolderTags.Personal, match), StringComparison.Ordinal);
    }

    [Fact]
    public void Describe_TagsAndRule_AreJoined()
    {
        FolderRuleMatch match = MatchFor(CacheRule("sample-rule", @"{Profile}\Sample", "temp"), "Sample");

        Assert.Equal("Inaccessible, Temp", FolderTagText.Describe(FolderTags.Inaccessible, match));
        Assert.Equal("Temp", FolderTagText.Describe(FolderTags.None, match));
        Assert.Equal("Inaccessible", FolderTagText.Describe(FolderTags.Inaccessible, null));
    }

    [Fact]
    public void Explain_Rule_ListsLabelExplanationAndDescriptionsPrimaryFirst()
    {
        FolderRuleMatch match = MatchFor(
            CacheRule("broad-rule", @"{Profile}\*", "cache"),
            "Sample",
            CacheRule("exact-rule", @"{Profile}\Sample", "temp"));

        string[] lines = FolderTagText.Explain(FolderTags.None, match).Split('\n');

        Assert.StartsWith("Temp:", lines[0], StringComparison.Ordinal);
        Assert.Equal("• Rule exact-rule.", lines[1]);
        Assert.Equal("• Rule broad-rule.", lines[2]);
        Assert.Equal(3, lines.Length);
    }

    [Fact]
    public void Explain_InheritedProtectedRule_SaysItComesFromAParentFolder()
    {
        FolderRuleMatch match = MatchFor(
            """{ "id": "sample-protected", "kind": "protected", "paths": ["{Profile}\\Sample"], "scope": "subtree", "description": "Keys.", "source": "s" }""",
            @"Sample\Child");

        string text = FolderTagText.Explain(FolderTags.None, match);

        Assert.Contains("Protected:", text, StringComparison.Ordinal);
        Assert.Contains("• Keys. (rule of a parent folder)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Explain_NothingToShow_IsEmpty()
    {
        Assert.Equal(string.Empty, FolderTagText.Explain(FolderTags.None, null));
    }
}

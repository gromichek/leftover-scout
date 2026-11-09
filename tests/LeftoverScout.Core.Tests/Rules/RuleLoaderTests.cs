using LeftoverScout.Core.Rules;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;

namespace LeftoverScout.Core.Tests.Rules;

public class RuleLoaderTests
{
    private static void AssertSkipped(RuleLoadResult result, string ruleId, string messagePart)
    {
        Assert.DoesNotContain(result.RuleSet.Rules, rule => rule.Id == ruleId);
        Assert.Contains(result.Errors, error => error.RuleId == ruleId && error.Message.Contains(messagePart, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Load_ValidRules_BuildsTheRuleSet()
    {
        RuleLoadResult result = Load(
            Protected("p-one", @"{Profile}\AppData", "folder"),
            Cache("c-one", @"{LocalAppData}\Temp", "temp"),
            Owner("o-one", @"{RoamingAppData}\sample"));

        Assert.Empty(result.Errors);
        Assert.Equal(["p-one", "c-one", "o-one"], result.RuleSet.Rules.Select(rule => rule.Id));
        Assert.Equal(RuleScope.Folder, result.RuleSet.Rules[0].Scope);
        Assert.Equal(CacheCategory.Temp, result.RuleSet.Rules[1].Category);
        Assert.Equal("Sample App", result.RuleSet.Rules[2].Program!.DisplayName);
    }

    [Fact]
    public void Load_CacheAndOwnershipWithoutScope_DefaultToSubtree()
    {
        RuleLoadResult result = Load(Cache("c-one", @"{LocalAppData}\Temp", "temp"), Owner("o-one", @"{RoamingAppData}\sample"));

        Assert.All(result.RuleSet.Rules, rule => Assert.Equal(RuleScope.Subtree, rule.Scope));
    }

    [Fact]
    public void Load_ProgramHints_AreKept()
    {
        RuleLoadResult result = Load("""
            { "id": "o-hints", "kind": "ownership", "paths": ["{RoamingAppData}\\sample"],
              "program": { "displayName": "Sample", "requiresInstallation": true, "names": ["Sample App"], "publishers": ["Sample Inc."],
                           "uninstallKeys": ["SampleKey"], "packageFamilies": ["Sample.Family_abc"], "programFolders": ["Sample"] },
              "description": "d", "source": "s" }
            """);

        Assert.Empty(result.Errors);
        ProgramHints hints = result.RuleSet.Rules.Single().Program!;
        Assert.Equal(["Sample App"], hints.Names);
        Assert.Equal(["Sample Inc."], hints.Publishers);
        Assert.Equal(["SampleKey"], hints.UninstallKeys);
        Assert.Equal(["Sample.Family_abc"], hints.PackageFamilies);
        Assert.Equal(["Sample"], hints.ProgramFolders);
    }

    [Fact]
    public void Load_UnknownKind_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "bad-kind", "kind": "delete", "paths": ["{Profile}\\a"], "description": "d", "source": "s" }""", Cache("good-one", @"{Profile}\a"));

        AssertSkipped(result, "bad-kind", "Unknown kind");
        Assert.Single(result.RuleSet.Rules);
    }

    [Fact]
    public void Load_UnknownCategory_SkipsTheRule()
    {
        AssertSkipped(Load(Cache("bad-category", @"{Profile}\a", "junk")), "bad-category", "Unknown category");
    }

    [Fact]
    public void Load_UnknownScope_SkipsTheRule()
    {
        AssertSkipped(Load(Protected("bad-scope", @"{Profile}\a", "everything")), "bad-scope", "Unknown scope");
    }

    [Fact]
    public void Load_UnknownToken_SkipsTheRule()
    {
        AssertSkipped(Load(Cache("bad-token", @"{Documents}\a")), "bad-token", "Unknown token");
    }

    [Theory]
    [InlineData(@"C:\Users\x\AppData")]
    [InlineData(@"{Profile}\..\x")]
    [InlineData(@"{Profile}\Cache*")]
    [InlineData(@"{Profile}\a\\b")]
    [InlineData(@"%APPDATA%\x")]
    public void Load_BadPattern_SkipsTheRule(string path)
    {
        RuleLoadResult result = Load(Cache("bad-path", path));

        Assert.Empty(result.RuleSet.Rules);
        Assert.Contains(result.Errors, error => error.RuleId == "bad-path");
    }

    [Fact]
    public void Load_DuplicateId_SkipsTheSecondRule()
    {
        RuleLoadResult result = Load(Cache("same-id", @"{Profile}\a"), Cache("same-id", @"{Profile}\b"));

        Assert.Single(result.RuleSet.Rules);
        Assert.Equal(@"{Profile}\a", result.RuleSet.Rules[0].Paths[0].Text);
        Assert.Contains(result.Errors, error => error.RuleId == "same-id" && error.Message.Contains("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_DuplicateIdInAnotherFile_SkipsTheSecondRule()
    {
        RuleFile first = File(Cache("same-id", @"{Profile}\a"));
        RuleFile second = new("other.json", File(Cache("same-id", @"{Profile}\b")).Json);

        RuleLoadResult result = RuleLoader.Load([first, second]);

        Assert.Single(result.RuleSet.Rules);
        Assert.Contains(result.Errors, error => error.File == "other.json" && error.RuleId == "same-id");
    }

    [Theory]
    [InlineData("Bad_Id")]
    [InlineData("UPPER")]
    [InlineData("double--hyphen")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("has space")]
    public void Load_IdNotKebabCase_SkipsTheRule(string id)
    {
        RuleLoadResult result = Load(Cache(id, @"{Profile}\a"));

        Assert.Empty(result.RuleSet.Rules);
        Assert.Contains(result.Errors, error => error.Message.Contains("kebab-case", StringComparison.Ordinal));
    }

    [Fact]
    public void Load_ProtectedWithoutScope_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "no-scope", "kind": "protected", "paths": ["{Profile}\\a"], "description": "d", "source": "s" }""");

        AssertSkipped(result, "no-scope", "needs a scope");
    }

    [Fact]
    public void Load_CacheWithoutCategory_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "no-category", "kind": "cache", "paths": ["{Profile}\\a"], "description": "d", "source": "s" }""");

        AssertSkipped(result, "no-category", "needs a category");
    }

    [Fact]
    public void Load_OwnershipWithoutProgram_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "no-program", "kind": "ownership", "paths": ["{Profile}\\a"], "description": "d", "source": "s" }""");

        AssertSkipped(result, "no-program", "needs a program");
    }

    [Fact]
    public void Load_OwnershipWithoutRequiresInstallation_SkipsTheRule()
    {
        RuleLoadResult result = Load("""
            { "id": "no-flag", "kind": "ownership", "paths": ["{Profile}\\a"],
              "program": { "displayName": "Sample" }, "description": "d", "source": "s" }
            """);

        AssertSkipped(result, "no-flag", "requiresInstallation");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Load_RequiresInstallation_IsKept(bool value)
    {
        RuleLoadResult result = Load(Owner("o-flag", @"{Profile}\a", requiresInstallation: value));

        Assert.Empty(result.Errors);
        Assert.Equal(value, result.RuleSet.Rules.Single().Program!.RequiresInstallation);
    }

    [Fact]
    public void Load_CacheProgramWithoutRequiresInstallation_IsValid()
    {
        RuleLoadResult result = Load("""
            { "id": "c-flag", "kind": "cache", "paths": ["{Profile}\\a"], "category": "cache",
              "program": { "displayName": "Sample" }, "description": "d", "source": "s" }
            """);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Load_ProgramWithoutDisplayName_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "no-name", "kind": "ownership", "paths": ["{Profile}\\a"], "program": { "requiresInstallation": true, "names": ["x"] }, "description": "d", "source": "s" }""");

        AssertSkipped(result, "no-name", "displayName");
    }

    [Theory]
    [InlineData("names")]
    [InlineData("publishers")]
    [InlineData("uninstallKeys")]
    [InlineData("packageFamilies")]
    [InlineData("programFolders")]
    public void Load_ProgramHintWithWildcard_SkipsTheRule(string list)
    {
        RuleLoadResult result = Load($$"""
            { "id": "wild-hint", "kind": "ownership", "paths": ["{Profile}\\a"],
              "program": { "displayName": "Sample", "requiresInstallation": true, "{{list}}": ["Sam*"] }, "description": "d", "source": "s" }
            """);

        AssertSkipped(result, "wild-hint", "wildcard");
    }

    [Fact]
    public void Load_NoPaths_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "no-paths", "kind": "cache", "paths": [], "category": "cache", "description": "d", "source": "s" }""");

        AssertSkipped(result, "no-paths", "at least one path");
    }

    [Fact]
    public void Load_MissingDescriptionOrSource_SkipsTheRule()
    {
        RuleLoadResult result = Load(
            """{ "id": "no-description", "kind": "cache", "paths": ["{Profile}\\a"], "category": "cache", "source": "s" }""",
            """{ "id": "no-source", "kind": "cache", "paths": ["{Profile}\\a"], "category": "cache", "description": "d" }""");

        AssertSkipped(result, "no-description", "description");
        AssertSkipped(result, "no-source", "source");
    }

    [Fact]
    public void Load_CategoryOnNonCacheRule_SkipsTheRule()
    {
        RuleLoadResult result = Load("""{ "id": "odd-category", "kind": "protected", "paths": ["{Profile}\\a"], "scope": "folder", "category": "cache", "description": "d", "source": "s" }""");

        AssertSkipped(result, "odd-category", "Only cache rules");
    }

    [Fact]
    public void Load_MisspelledProperty_SkipsTheRuleAndKeepsTheOthers()
    {
        RuleLoadResult result = Load(
            """{ "id": "typo-rule", "kind": "cache", "paths": ["{Profile}\\a"], "categroy": "cache", "description": "d", "source": "s" }""",
            Cache("good-one", @"{Profile}\a"));

        Assert.Equal(["good-one"], result.RuleSet.Rules.Select(rule => rule.Id));
        Assert.Single(result.Errors);
    }

    [Theory]
    [InlineData("{ \"schemaVersion\": 2, \"rules\": [] }")]
    [InlineData("{ \"rules\": [] }")]
    [InlineData("{ \"schemaVersion\": 1 }")]
    [InlineData("{ \"schemaVersion\": 1, \"rules\": [], \"extra\": 1 }")]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("null")]
    public void Load_BrokenFile_ReportsAFileErrorAndNeverThrows(string json)
    {
        RuleLoadResult result = RuleLoader.Load([new RuleFile("broken.json", json)]);

        Assert.Empty(result.RuleSet.Rules);
        Assert.Contains(result.Errors, error => error.File == "broken.json" && error.RuleId is null);
    }

    [Fact]
    public void Load_BrokenFile_DoesNotStopOtherFiles()
    {
        RuleLoadResult result = RuleLoader.Load([new RuleFile("broken.json", "not json"), File(Cache("good-one", @"{Profile}\a"))]);

        Assert.Single(result.RuleSet.Rules);
    }

    [Theory]
    [InlineData(@"{Profile}\AppData\Roaming\Microsoft\Protect")]
    [InlineData(@"{RoamingAppData}\Microsoft\Protect\keys")]
    [InlineData(@"{RoamingAppData}\Microsoft\*")]
    public void Load_CacheInsideProtectedSubtree_IsReported(string path)
    {
        RuleLoadResult result = Load(Protected("protect-keys", @"{RoamingAppData}\Microsoft\Protect", "subtree"), Cache("cache-inside", path));

        AssertSkipped(result, "cache-inside", "protected subtree");
        Assert.Contains(result.RuleSet.Rules, rule => rule.Id == "protect-keys");
    }

    [Fact]
    public void Load_OwnershipInsideProtectedSubtree_IsReported()
    {
        RuleLoadResult result = Load(Protected("protect-keys", @"{RoamingAppData}\Microsoft\Protect", "subtree"), Owner("owner-inside", @"{RoamingAppData}\Microsoft\Protect\x"));

        AssertSkipped(result, "owner-inside", "protected subtree");
    }

    [Fact]
    public void Load_RuleDeclaredBeforeTheProtectedRule_IsStillReported()
    {
        RuleLoadResult result = Load(Cache("cache-inside", @"{Profile}\Secret\x"), Protected("protect-secret", @"{Profile}\Secret", "subtree"));

        AssertSkipped(result, "cache-inside", "protected subtree");
    }

    [Fact]
    public void Load_RuleInsideFolderScopeProtection_IsAllowed()
    {
        RuleLoadResult result = Load(Protected("protect-local", @"{LocalAppData}", "folder"), Cache("temp-rule", @"{LocalAppData}\Temp", "temp"));

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.RuleSet.Rules.Count);
    }

    [Fact]
    public void Load_ProtectedRuleInsideAnotherProtectedSubtree_IsAllowed()
    {
        RuleLoadResult result = Load(Protected("protect-a", @"{Profile}\a", "subtree"), Protected("protect-b", @"{Profile}\a\b", "folder"));

        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Load_UnrelatedRuleNextToProtectedSubtree_IsAllowed()
    {
        RuleLoadResult result = Load(Protected("protect-keys", @"{RoamingAppData}\Microsoft\Protect", "subtree"), Cache("cache-next", @"{RoamingAppData}\Microsoft\Teams\Cache"));

        Assert.Empty(result.Errors);
    }
}

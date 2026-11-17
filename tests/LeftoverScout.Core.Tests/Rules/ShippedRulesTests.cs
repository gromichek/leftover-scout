using LeftoverScout.Core.Rules;

namespace LeftoverScout.Core.Tests.Rules;

/// <summary>Checks the rule files that ship with the app (the repository folder rules/).</summary>
public class ShippedRulesTests
{
    private static readonly RuleLoadResult Result = RuleLoader.Load(EmbeddedRuleFiles.Read());

    [Fact]
    public void ShippedFiles_AreEmbedded()
    {
        Assert.Equal(["caches.json", "ownership.json", "protected.json"], EmbeddedRuleFiles.Read().Select(file => file.Name));
    }

    [Fact]
    public void ShippedRules_LoadWithoutErrors()
    {
        Assert.Empty(Result.Errors);
    }

    [Fact]
    public void ShippedRules_CountIsInExpectedRange()
    {
        Assert.InRange(Result.RuleSet.Rules.Count, 60, 100);
    }

    [Fact]
    public void ShippedRules_HaveDescriptionAndSource()
    {
        Assert.All(Result.RuleSet.Rules, rule =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Description), rule.Id);
            Assert.False(string.IsNullOrWhiteSpace(rule.Source), rule.Id);
        });
    }

    [Fact]
    public void ShippedRules_OnlyToolsWithoutInstallerEntryDoNotRequireInstallation()
    {
        string[] withoutInstaller = ["gradle-data", "npm-global", "nuget-config"];
        Assert.All(
            Result.RuleSet.Rules.Where(rule => rule.Kind == RuleKind.Ownership),
            rule => Assert.True(rule.Program!.RequiresInstallation == !withoutInstaller.Contains(rule.Id), rule.Id));
    }

    [Theory]
    [InlineData("protected-ssh", @"{Profile}\.ssh", RuleScope.Subtree)]
    [InlineData("protected-gnupg", @"{RoamingAppData}\gnupg", RuleScope.Subtree)]
    [InlineData("protected-aws", @"{Profile}\.aws", RuleScope.Subtree)]
    [InlineData("protected-azure", @"{Profile}\.azure", RuleScope.Subtree)]
    [InlineData("protected-kube", @"{Profile}\.kube", RuleScope.Subtree)]
    [InlineData("protected-docker-config", @"{Profile}\.docker", RuleScope.Subtree)]
    [InlineData("protected-android", @"{Profile}\.android", RuleScope.Subtree)]
    [InlineData("protected-crypto-wallets", @"{RoamingAppData}\Exodus", RuleScope.Subtree)]
    [InlineData("protected-package-cache", @"{LocalAppData}\Package Cache", RuleScope.Subtree)]
    [InlineData("protected-virtual-store", @"{LocalAppData}\VirtualStore", RuleScope.Subtree)]
    [InlineData("protected-isolated-storage", @"{LocalAppData}\IsolatedStorage", RuleScope.Subtree)]
    [InlineData("protected-windows-local-folders", @"{LocalAppData}\Comms", RuleScope.Subtree)]
    [InlineData("protected-d3d-shader-cache", @"{LocalAppData}\D3DSCache", RuleScope.Folder)]
    public void ShippedRules_ProtectCredentialsWalletsAndWindowsFolders(string id, string path, RuleScope scope)
    {
        Rule rule = Result.RuleSet.Rules.Single(item => item.Id == id);
        Assert.Equal(RuleKind.Protected, rule.Kind);
        Assert.Equal(scope, rule.Scope);
        Assert.Contains(rule.Paths, p => p.Text == path);
    }

    [Fact]
    public void ShippedRules_SpotifyCacheWarnsAboutDownloadingAgain()
    {
        Rule rule = Result.RuleSet.Rules.Single(item => item.Id == "spotify-cache");
        Assert.Contains("download", rule.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("again", rule.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShippedRules_HaveUniqueIds()
    {
        Assert.Equal(Result.RuleSet.Rules.Count, Result.RuleSet.Rules.Select(rule => rule.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(@"{Profile}\AppData")]
    [InlineData(@"{RoamingAppData}")]
    [InlineData(@"{LocalAppData}")]
    [InlineData(@"{LocalLow}")]
    public void ShippedRules_ProtectTheAppDataRootsAsFolders(string path)
    {
        Rule rule = Result.RuleSet.Rules.Single(item => item.Kind == RuleKind.Protected && item.Paths.Any(p => p.Text == path));
        Assert.Equal(RuleScope.Folder, rule.Scope);
    }

    [Theory]
    [InlineData(@"{RoamingAppData}\Microsoft\Protect")]
    [InlineData(@"{RoamingAppData}\Microsoft\Credentials")]
    [InlineData(@"{LocalAppData}\Microsoft\Credentials")]
    [InlineData(@"{RoamingAppData}\Microsoft\SystemCertificates")]
    [InlineData(@"{RoamingAppData}\Microsoft\Crypto")]
    [InlineData(@"{LocalAppData}\Microsoft\Vault")]
    [InlineData(@"{RoamingAppData}\Microsoft\Windows\Start Menu")]
    [InlineData(@"{LocalAppData}\Microsoft\WindowsApps")]
    public void ShippedRules_ProtectWindowsDataAsSubtrees(string path)
    {
        Rule rule = Result.RuleSet.Rules.Single(item => item.Kind == RuleKind.Protected && item.Paths.Any(p => p.Text == path));
        Assert.Equal(RuleScope.Subtree, rule.Scope);
    }

    [Fact]
    public void ShippedRules_OwnershipRulesHaveProgramNames()
    {
        Assert.All(
            Result.RuleSet.Rules.Where(rule => rule.Kind == RuleKind.Ownership),
            rule => Assert.NotEmpty(rule.Program!.Names));
    }

    [Fact]
    public void ShippedRules_NoCacheOrOwnershipRuleReachesIntoProtectedSubtrees()
    {
        List<PathPattern> protectedSubtrees = Result.RuleSet.Rules
            .Where(rule => rule.Kind == RuleKind.Protected && rule.Scope == RuleScope.Subtree)
            .SelectMany(rule => rule.Paths)
            .ToList();

        foreach (Rule rule in Result.RuleSet.Rules.Where(rule => rule.Kind != RuleKind.Protected))
        {
            foreach (PathPattern path in rule.Paths)
            {
                Assert.DoesNotContain(protectedSubtrees, protectedPath => path.MayLieInside(protectedPath));
            }
        }
    }

    [Fact]
    public void EmbeddedRuleLoader_LoadsTheShippedRules()
    {
        RuleSet ruleSet = EmbeddedRuleLoader.Load(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        Assert.Equal(Result.RuleSet.Rules.Count, ruleSet.Rules.Count);
    }
}

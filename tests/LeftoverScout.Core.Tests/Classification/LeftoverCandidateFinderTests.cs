using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Rules;
using static LeftoverScout.Core.Tests.Classification.Evidences;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Classification;

public class LeftoverCandidateFinderTests
{
    private static readonly DateTime Old = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static string[] Find(
        TreeBuilder tree,
        EvidenceSnapshot? evidence = null,
        IReadOnlyList<string>? personal = null,
        params string[] ruleJson)
    {
        FolderNode root = tree.Build(personal);
        RuleSet rules = ruleJson.Length == 0 ? RuleSet.Empty : RuleTestHelper.Load(ruleJson).RuleSet;
        RuleMatchMap matches = new RuleMatcher(rules).Match(root, TestContext.Current.CancellationToken);
        var index = new EvidenceIndex(evidence ?? Snapshot());

        return [.. LeftoverCandidateFinder.Find(root, index, matches)
            .Select(candidate => $"{candidate.Node.Name}:{candidate.Kind}")
            .Order(StringComparer.Ordinal)];
    }

    [Fact]
    public void Find_FirstLevelOfRoamingLocalAndLocalLow_AreCandidates()
    {
        TreeBuilder tree = new TreeBuilder()
            .Dir(@"AppData\Roaming\AlphaTool")
            .Dir(@"AppData\Local\BetaTool")
            .Dir(@"AppData\LocalLow\GammaTool");

        Assert.Equal(
            ["AlphaTool:AppDataFolder", "BetaTool:AppDataFolder", "GammaTool:AppDataFolder"],
            Find(tree));
    }

    [Fact]
    public void Find_SecondLevelBelowANormalFolder_AndOtherAppDataFolders_AreNotCandidates()
    {
        TreeBuilder tree = new TreeBuilder()
            .Dir(@"AppData\Roaming\AlphaTool\Inner")
            .Dir(@"AppData\Other\Skipped")
            .Dir(@"Work\Project")
            .Dir(@"Plain");

        Assert.Equal(["AlphaTool:AppDataFolder"], Find(tree));
    }

    [Fact]
    public void Find_DotFoldersInProfileRoot_AreCandidates()
    {
        TreeBuilder tree = new TreeBuilder()
            .Dir(".delta")
            .Dir(@".epsilon\Inner")
            .Dir(@"Work\.hidden");

        Assert.Equal([".delta:DotFolder", ".epsilon:DotFolder"], Find(tree));
    }

    [Fact]
    public void Find_EveryFolderInLocalPackages_IsAStorePackageCandidate()
    {
        TreeBuilder tree = new TreeBuilder()
            .Dir(@"AppData\Local\Packages\Contoso.One_abc123")
            .Dir(@"AppData\Local\Packages\Contoso.Two_abc123\LocalState")
            .Dir(@"AppData\Roaming\Packages\NotAStorePackage");

        Assert.Equal(
            ["Contoso.One_abc123:StorePackage", "Contoso.Two_abc123:StorePackage", "Packages:AppDataFolder"],
            Find(tree));
    }

    [Fact]
    public void Find_VendorFolder_IsReplacedByItsSubfolders()
    {
        TreeBuilder tree = new TreeBuilder()
            .Dir(@"AppData\Local\Contoso\ProductA")
            .Dir(@"AppData\Local\Contoso\ProductB")
            .Dir(@"AppData\Local\Contoso\ProductB\Deep");

        string[] candidates = Find(tree, Snapshot(Registry("Some Suite", publisher: "Contoso Ltd")));

        Assert.Equal(["ProductA:VendorChild", "ProductB:VendorChild"], candidates);
    }

    [Fact]
    public void Find_FolderThatMatchesAProgramName_IsNotExpanded()
    {
        TreeBuilder tree = new TreeBuilder().Dir(@"AppData\Local\Contoso\ProductA");

        string[] candidates = Find(tree, Snapshot(Registry("Contoso", publisher: "Contoso Ltd")));

        Assert.Equal(["Contoso:AppDataFolder"], candidates);
    }

    [Fact]
    public void Find_MicrosoftFolder_IsNeverExpandedIntoNormalCandidates()
    {
        TreeBuilder tree = new TreeBuilder()
            .Dir(@"AppData\Roaming\Microsoft\Windows")
            .Dir(@"AppData\Local\Microsoft\Edge");

        string[] candidates = Find(tree, Snapshot(Registry("Some Product", publisher: "Microsoft Corporation")));

        Assert.Equal(["Edge:MicrosoftChild", "Windows:MicrosoftChild"], candidates);
    }

    [Fact]
    public void Find_PersonalProtectedCacheLinkCloudAndInaccessibleFolders_AreNotCandidates()
    {
        string roaming = Root + @"\AppData\Roaming";
        TreeBuilder tree = new TreeBuilder()
            .Dir(@"AppData\Roaming\Personal1")
            .Dir(@"AppData\Roaming\Prot")
            .Dir(@"AppData\Roaming\CacheFolder")
            .Dir(@"AppData\Roaming\DeniedFolder")
            .Dir(@"AppData\Roaming\Normal")
            .Raw(@"AppData\Roaming", JunctionEntry("LinkFolder"))
            .Raw(@"AppData\Roaming", CloudDirEntry("CloudFolder"))
            .Denied(@"AppData\Roaming\DeniedFolder");

        string[] candidates = Find(
            tree,
            personal: [roaming + @"\Personal1"],
            ruleJson:
            [
                RuleTestHelper.Protected("prot-rule", @"{RoamingAppData}\Prot", "subtree"),
                RuleTestHelper.Cache("cache-rule", @"{RoamingAppData}\CacheFolder"),
            ]);

        Assert.Equal(["Normal:AppDataFolder"], candidates);
    }

    [Fact]
    public void Find_OwnershipRuleFolder_RemainsACandidate()
    {
        TreeBuilder tree = new TreeBuilder().Dir(@"AppData\Roaming\Owned");

        string[] candidates = Find(tree, ruleJson: [RuleTestHelper.Owner("owner-rule", @"{RoamingAppData}\Owned")]);

        Assert.Equal(["Owned:AppDataFolder"], candidates);
    }

    [Fact]
    public void Find_PersonalFolderInProfileRoot_IsNotADotFolderCandidate()
    {
        TreeBuilder tree = new TreeBuilder().Dir("Documents").File("Documents", "a.txt", Old);

        Assert.Empty(Find(tree));
    }
}

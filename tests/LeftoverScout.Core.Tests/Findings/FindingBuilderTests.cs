using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Classification;
using LeftoverScout.Core.Tests.Rules;
using static LeftoverScout.Core.Tests.Findings.FindingTestKit;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Findings;

public class FindingBuilderTests
{
    private static readonly DateTime Recent = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Finding Single(IReadOnlyList<Finding> findings, string relativePath) =>
        findings.Single(finding => finding.RelativePath == relativePath);

    [Fact]
    public void Build_ScanWithoutTree_ReturnsNothing()
    {
        var result = new ScanResult(ScanCompletion.Cancelled, null, [], [], 0, 0, 0, TimeSpan.Zero);

        Assert.Empty(FindingBuilder.Build(result));
    }

    [Fact]
    public void Build_LeftoverVerdicts_AreListedWithConfidenceSizeAndRelativePath()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\FooSoft", "a.dat", Old, 100)
            .File(@"AppData\Roaming\BarTool", "b.dat", Recent, 40)
            .Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(root));

        Finding foo = Single(findings, @"AppData\Roaming\FooSoft");
        Assert.Equal(FindingKind.Leftover, foo.Kind);
        Assert.Equal(LeftoverConfidence.Medium, foo.Confidence);
        Assert.Equal("Medium", foo.ConfidenceText);
        Assert.Equal("Leftover", foo.KindText);
        Assert.Equal(100, foo.LocalSize);
        Assert.Equal(1, foo.FileCount);
        Assert.Equal(Old, foo.NewestWriteTimeUtc);
        Assert.Contains("No installed program named 'FooSoft' was found.", foo.Explanation[0], StringComparison.Ordinal);
        Assert.Equal(LeftoverConfidence.Low, Single(findings, @"AppData\Roaming\BarTool").Confidence);
        Assert.Equal(2, findings.Count);
    }

    [Fact]
    public void Build_InUseAndUnknownVerdicts_AreNotListed()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\Cache", "a.dat", Old, 100)
            .File(@"AppData\Roaming\Unrelated Program", "b.dat", Old, 100)
            .Build();

        Assert.Empty(FindingBuilder.Build(Result(root)));
    }

    [Fact]
    public void Build_CacheRule_IsListedWithKindAndRuleText()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Local\SampleLogs", "a.log", Old, 70).Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(root, Cache("sample-logs", @"{LocalAppData}\SampleLogs", "logs")));

        Finding logs = Assert.Single(findings);
        Assert.Equal(FindingKind.Logs, logs.Kind);
        Assert.Equal("Logs", logs.KindText);
        Assert.Null(logs.Confidence);
        Assert.Equal(string.Empty, logs.ConfidenceText);
        Assert.Equal("sample-logs", logs.RuleId);
        Assert.Equal("d", logs.RuleDescription);
        Assert.Equal(@"AppData\Local\SampleLogs", logs.RelativePath);
    }

    [Theory]
    [InlineData("cache", FindingKind.Cache)]
    [InlineData("temp", FindingKind.Temp)]
    [InlineData("logs", FindingKind.Logs)]
    [InlineData("crashDumps", FindingKind.CrashDumps)]
    public void Build_CacheCategories_MapToKinds(string category, FindingKind expected)
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Local\SampleData", "a.bin", Old, 10).Build();

        Finding finding = Assert.Single(FindingBuilder.Build(Result(root, Cache("c", @"{LocalAppData}\SampleData", category))));

        Assert.Equal(expected, finding.Kind);
    }

    [Fact]
    public void Build_CacheInsideListedCache_OnlyTopMostIsListed()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Local\SampleCache", "a.bin", Old, 10)
            .File(@"AppData\Local\SampleCache\Inner", "b.bin", Old, 20)
            .Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(root, Cache("c", @"{LocalAppData}\SampleCache", scope: "subtree")));

        Finding outer = Assert.Single(findings);
        Assert.Equal(@"AppData\Local\SampleCache", outer.RelativePath);
        Assert.Equal(30, outer.LocalSize);
    }

    [Fact]
    public void Build_CacheInsideLeftover_IsListedAsWell()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\FooSoft", "a.dat", Old, 100)
            .File(@"AppData\Roaming\FooSoft\Cache", "b.bin", Old, 40)
            .Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(root, Cache("c", @"{RoamingAppData}\*\Cache")));

        Assert.Equal(FindingKind.Leftover, Single(findings, @"AppData\Roaming\FooSoft").Kind);
        Assert.Equal(FindingKind.Cache, Single(findings, @"AppData\Roaming\FooSoft\Cache").Kind);
    }

    [Fact]
    public void Build_FolderThatIsLeftoverAndCache_IsListedOnceAsLeftover()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\FooSoft", "a.dat", Old, 100)
            .File(@"AppData\Roaming\FooSoft\Inner", "b.bin", Old, 5)
            .Build();

        // The classifier never rates a cache folder, so the verdict is made by hand.
        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root,
            [Cache("c", @"{RoamingAppData}\FooSoft", "temp", "subtree")],
            (@"AppData\Roaming\FooSoft", Leftover(LeftoverConfidence.High))));

        Finding foo = Assert.Single(findings);
        Assert.Equal(FindingKind.Leftover, foo.Kind);
        Assert.Equal(LeftoverConfidence.High, foo.Confidence);
        Assert.Contains(foo.Explanation, line => line.StartsWith("It also matches a cache rule:", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_ProtectedFolder_IsNeverListed_EvenWithLeftoverVerdictOrCacheRule()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\Vault", "a.dat", Old, 100)
            .File(@"AppData\Roaming\Other", "b.dat", Old, 100)
            .Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root,
            [Protected("p", @"{RoamingAppData}\Vault", "folder"), Cache("c", @"{RoamingAppData}\Vault")],
            (@"AppData\Roaming\Vault", Leftover()),
            (@"AppData\Roaming\Other", Leftover())));

        Assert.Equal(@"AppData\Roaming\Other", Assert.Single(findings).RelativePath);
    }

    [Fact]
    public void Build_ProtectedAncestorWithSubtreeScope_BlocksEverythingBelow()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\Secrets\Deep\Inner", "a.dat", Old, 100).Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root,
            [Protected("p", @"{RoamingAppData}\Secrets", "subtree")],
            (@"AppData\Roaming\Secrets\Deep", Leftover())));

        Assert.Empty(findings);
    }

    [Fact]
    public void Build_ProtectedAncestorWithFolderScope_DoesNotBlockChildren()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\FooSoft", "a.dat", Old, 100).Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root,
            [Protected("p1", @"{Profile}\AppData", "folder"), Protected("p2", @"{RoamingAppData}", "folder")],
            (@"AppData\Roaming\FooSoft", Leftover())));

        Assert.Single(findings);
    }

    [Fact]
    public void Build_PersonalFolderAndItsAncestorPersonalChildren_AreNeverListed()
    {
        FolderNode root = new TreeBuilder()
            .File("Documents", "a.dat", Old, 100)
            .File(@"Documents\FooSoft", "b.dat", Old, 100)
            .File(@"Documents\FooSoft\Cache", "c.dat", Old, 100)
            .File(@"AppData\Roaming\FooSoft", "d.dat", Old, 100)
            .Build([Root + @"\Documents"]);

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root,
            [Cache("c", @"{Profile}\Documents\FooSoft\Cache")],
            ("Documents", Leftover()),
            (@"Documents\FooSoft", Leftover()),
            (@"AppData\Roaming\FooSoft", Leftover())));

        Assert.Equal(@"AppData\Roaming\FooSoft", Assert.Single(findings).RelativePath);
    }

    [Fact]
    public void Build_LinkCloudAndEmptyFolders_AreNeverListed()
    {
        FolderNode root = new TreeBuilder()
            .Raw(@"AppData\Roaming", JunctionEntry("LinkSoft"))
            .Raw(@"AppData\Roaming", CloudDirEntry("CloudSoft"))
            .Dir(@"AppData\Roaming\EmptySoft")
            .File(@"AppData\Roaming\RealSoft", "a.dat", Old, 10)
            .Build();

        IReadOnlyList<Finding> findings = FindingBuilder.Build(Result(
            root,
            [Cache("c", @"{RoamingAppData}\EmptySoft")],
            (@"AppData\Roaming\LinkSoft", Leftover()),
            (@"AppData\Roaming\CloudSoft", Leftover()),
            (@"AppData\Roaming\EmptySoft", Leftover()),
            (@"AppData\Roaming\RealSoft", Leftover())));

        Assert.Equal(@"AppData\Roaming\RealSoft", Assert.Single(findings).RelativePath);
    }

    [Fact]
    public void Build_OwnershipRule_GivesProgramNameAndRule()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\FooSoft", "a.dat", Old, 10).Build();

        Finding finding = Assert.Single(FindingBuilder.Build(Result(
            root,
            [Owner("foo-owner", @"{RoamingAppData}\FooSoft", "Foo Soft")],
            (@"AppData\Roaming\FooSoft", Leftover(LeftoverConfidence.High)))));

        Assert.Equal("Foo Soft", finding.ProgramName);
        Assert.Equal("foo-owner", finding.RuleId);
    }
}

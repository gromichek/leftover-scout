using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Rules;
using static LeftoverScout.Core.Tests.Classification.Evidences;

namespace LeftoverScout.Core.Tests.Classification;

public class LeftoverClassifierTests
{
    // "Now" is 2026-11-09; six months earlier is 2026-05-09 (decision D-046).
    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Cutoff = new(2026, 5, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Old = new(2023, 5, 20, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Recent = new(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);

    private static readonly EvidenceSnapshot Unrelated = Snapshot(Registry("Unrelated Program"));

    private static LeftoverMap ClassifyAll(FolderNode root, EvidenceSnapshot? evidence, params string[] ruleJson)
    {
        RuleSet rules = ruleJson.Length == 0 ? RuleSet.Empty : RuleTestHelper.Load(ruleJson).RuleSet;
        RuleMatchMap matches = new RuleMatcher(rules).Match(root, TestContext.Current.CancellationToken);
        return new LeftoverClassifier(new FixedTimeProvider(new DateTimeOffset(Now))).Classify(root, evidence, matches, TestContext.Current.CancellationToken);
    }

    private static LeftoverVerdict? Classify(TreeBuilder tree, string folder, EvidenceSnapshot? evidence = null, params string[] ruleJson)
    {
        FolderNode root = tree.Build();
        return ClassifyAll(root, evidence ?? Unrelated, ruleJson).Get(RuleTestHelper.Find(root, folder));
    }

    private static LeftoverVerdict? InRoaming(string name, DateTime lastWrite, EvidenceSnapshot? evidence = null, params string[] ruleJson) =>
        Classify(new TreeBuilder().File(@"AppData\Roaming\" + name, "a.dat", lastWrite), @"AppData\Roaming\" + name, evidence, ruleJson);

    private static LeftoverVerdict? InPackages(string family, DateTime lastWrite, EvidenceSnapshot? evidence = null) =>
        Classify(new TreeBuilder().File(@"AppData\Local\Packages\" + family, "a.dat", lastWrite), @"AppData\Local\Packages\" + family, evidence);

    private static void AssertLeftover(LeftoverVerdict? verdict, LeftoverConfidence confidence)
    {
        Assert.NotNull(verdict);
        Assert.Equal(LeftoverVerdictKind.Leftover, verdict.Kind);
        Assert.Equal(confidence, verdict.Confidence);
    }

    // --- Folders without evidence (section 6, Medium / Low) ---

    [Fact]
    public void Folder_NoEvidenceNotChangedForLong_IsMediumWithExplanation()
    {
        LeftoverVerdict? verdict = InRoaming("FooSoft", Old);

        AssertLeftover(verdict, LeftoverConfidence.Medium);
        Assert.Equal("No installed program named 'FooSoft' was found. Not changed since 2023-05.", verdict!.Explanation);
        Assert.Equal("Leftover: Medium", verdict.Label);
    }

    [Fact]
    public void Folder_NoEvidenceChangedExactlySixMonthsAgo_CountsAsNotChanged()
    {
        AssertLeftover(InRoaming("FooSoft", Cutoff), LeftoverConfidence.Medium);
    }

    [Fact]
    public void Folder_NoEvidenceChangedJustAfterTheThreshold_IsLow()
    {
        AssertLeftover(InRoaming("FooSoft", Cutoff.AddTicks(1)), LeftoverConfidence.Low);
    }

    [Fact]
    public void Folder_NoEvidenceChangedRecently_IsLowAndSaysSo()
    {
        LeftoverVerdict? verdict = InRoaming("FooSoft", Recent);

        AssertLeftover(verdict, LeftoverConfidence.Low);
        Assert.Contains("changed in 2026-10", verdict!.Explanation, StringComparison.Ordinal);
        Assert.Equal("Leftover: Low", verdict.Label);
    }

    [Fact]
    public void Folder_NoEvidenceAndNoFiles_IsMedium()
    {
        LeftoverVerdict? verdict = Classify(new TreeBuilder().Dir(@"AppData\Roaming\FooSoft"), @"AppData\Roaming\FooSoft");

        AssertLeftover(verdict, LeftoverConfidence.Medium);
        Assert.Contains("contains no files", verdict!.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Folder_NoEvidenceAndOnlyEmptySubfolders_IsMedium()
    {
        TreeBuilder tree = new TreeBuilder().Dir(@"AppData\Local\FooSoft\Empty\Deeper");

        AssertLeftover(Classify(tree, @"AppData\Local\FooSoft"), LeftoverConfidence.Medium);
    }

    // --- InUse ---

    [Fact]
    public void Folder_MatchingAnInstalledProgram_IsInUse()
    {
        LeftoverVerdict? verdict = InRoaming("FooSoft", Old, Snapshot(Registry("FooSoft 2022")));

        Assert.NotNull(verdict);
        Assert.Equal(LeftoverVerdictKind.InUse, verdict.Kind);
        Assert.Equal("FooSoft 2022", verdict.ProgramName);
        Assert.Equal("In use: FooSoft 2022", verdict.Label);
        Assert.Equal("Belongs to FooSoft 2022 (installed).", verdict.Explanation);
        Assert.Null(verdict.Confidence);
    }

    [Fact]
    public void Folder_MatchingOnlyAStartMenuEntry_IsInUse()
    {
        LeftoverVerdict? verdict = InRoaming("FooSoft", Old, Snapshot(StartMenu("FooSoft")));

        Assert.Equal(LeftoverVerdictKind.InUse, verdict!.Kind);
    }

    [Fact]
    public void Folder_SimilarButDifferentName_IsNotInUse()
    {
        AssertLeftover(InRoaming("Foo", Old, Snapshot(Registry("Food"))), LeftoverConfidence.Medium);
    }

    // --- Store packages ---

    [Fact]
    public void Package_NotInstalledAndNotChangedForLong_IsHigh()
    {
        LeftoverVerdict? verdict = InPackages("Contoso.Gone_abc123", Old);

        AssertLeftover(verdict, LeftoverConfidence.High);
        Assert.Equal("Store package 'Contoso.Gone_abc123' is not installed for this user. Not changed since 2023-05.", verdict!.Explanation);
        Assert.Equal("Leftover: High", verdict.Label);
    }

    [Fact]
    public void Package_NotInstalledButChangedRecently_IsMedium()
    {
        AssertLeftover(InPackages("Contoso.Gone_abc123", Recent), LeftoverConfidence.Medium);
    }

    [Fact]
    public void Package_ChangedExactlySixMonthsAgo_IsHigh_AndJustAfterIsMedium()
    {
        AssertLeftover(InPackages("Contoso.Gone_abc123", Cutoff), LeftoverConfidence.High);
        AssertLeftover(InPackages("Contoso.Gone_abc123", Cutoff.AddTicks(1)), LeftoverConfidence.Medium);
    }

    [Fact]
    public void Package_WithoutFiles_IsMedium()
    {
        LeftoverVerdict? verdict = Classify(new TreeBuilder().Dir(@"AppData\Local\Packages\Contoso.Gone_abc123"), @"AppData\Local\Packages\Contoso.Gone_abc123");

        AssertLeftover(verdict, LeftoverConfidence.Medium);
    }

    [Fact]
    public void Package_InstalledForTheUser_IsInUseEvenWhenOld()
    {
        LeftoverVerdict? verdict = InPackages("Contoso.Notes_abc123", Old, Snapshot(Store("Contoso Notes", "contoso.notes_ABC123")));

        Assert.Equal(LeftoverVerdictKind.InUse, verdict!.Kind);
        Assert.Equal("In use: Contoso Notes", verdict.Label);
        Assert.Contains("Contoso.Notes_abc123", verdict.Explanation, StringComparison.Ordinal);
    }

    // --- Ownership rules ---

    [Fact]
    public void Ownership_RequiresInstallationAndNoEvidence_IsHighEvenWhenChangedRecently()
    {
        LeftoverVerdict? verdict = InRoaming(
            "FooApp",
            Recent,
            ruleJson: [RuleTestHelper.Owner("foo-owner", @"{RoamingAppData}\FooApp", "FooSoft Studio")]);

        AssertLeftover(verdict, LeftoverConfidence.High);
        Assert.Contains("FooSoft Studio", verdict!.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void Ownership_DoesNotRequireInstallationAndNoEvidence_IsUnknown()
    {
        LeftoverVerdict? verdict = InRoaming(
            "FooApp",
            Old,
            ruleJson: [RuleTestHelper.Owner("foo-owner", @"{RoamingAppData}\FooApp", "FooSoft Studio", requiresInstallation: false)]);

        Assert.Equal(LeftoverVerdictKind.Unknown, verdict!.Kind);
        Assert.Equal(string.Empty, verdict.Label);
    }

    [Fact]
    public void Ownership_HintsMatchEvidence_IsInUseWithTheRuleProgram()
    {
        LeftoverVerdict? verdict = InRoaming(
            "FooApp",
            Old,
            Snapshot(Registry("Discord")),
            RuleTestHelper.Owner("foo-owner", @"{RoamingAppData}\FooApp", "Discord"));

        Assert.Equal(LeftoverVerdictKind.InUse, verdict!.Kind);
        Assert.Equal("Discord", verdict.ProgramName);
    }

    [Fact]
    public void Ownership_NameMatchesEvidenceButHintsDoNot_IsStillInUse()
    {
        LeftoverVerdict? verdict = InRoaming(
            "discord",
            Old,
            Snapshot(Registry("Discord Inc")),
            RuleTestHelper.Owner("o", @"{RoamingAppData}\discord", "Another Program Name"));

        Assert.Equal(LeftoverVerdictKind.InUse, verdict!.Kind);
        Assert.Equal("Discord Inc", verdict.ProgramName);
    }

    [Fact]
    public void Ownership_PublisherOnlyEvidence_DoesNotProveTheProgramIsInstalled()
    {
        LeftoverVerdict? verdict = InRoaming(
            "BazApp",
            Old,
            Snapshot(Registry("Other Product", publisher: "FooSoft Studio")),
            RuleTestHelper.Owner("foo-owner", @"{RoamingAppData}\BazApp", "FooSoft Studio"));

        AssertLeftover(verdict, LeftoverConfidence.High);
    }

    // --- Unknown ---

    [Theory]
    [InlineData("Cache")]
    [InlineData("Crashpad")]
    [InlineData("Temp")]
    [InlineData("CrashDumps")]
    [InlineData("Programs")]
    [InlineData("Microsoft Windows")]
    public void Folder_GenericName_IsUnknown(string name)
    {
        LeftoverVerdict? verdict = InRoaming(name, Old);

        Assert.Equal(LeftoverVerdictKind.Unknown, verdict!.Kind);
        Assert.Equal(string.Empty, verdict.Label);
    }

    [Theory]
    [InlineData("{3F2504E0-4F89-11D3-9A0C-0305E82C3301}")]
    [InlineData("3F2504E0-4F89-11D3-9A0C-0305E82C3301")]
    [InlineData("2022")]
    [InlineData("v2")]
    public void Folder_NameWithoutUsableTokens_IsUnknown(string name)
    {
        Assert.Equal(LeftoverVerdictKind.Unknown, InRoaming(name, Old)!.Kind);
    }

    [Fact]
    public void Microsoft_ChildrenAreUnknown_AndTheFolderItselfHasNoVerdict()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\Microsoft\Windows", "a.dat", Old).Build();

        LeftoverMap map = ClassifyAll(root, Snapshot(Registry("Some Product", publisher: "Microsoft Corporation")));

        Assert.Equal(LeftoverVerdictKind.Unknown, map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\Microsoft\Windows"))!.Kind);
        Assert.Null(map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\Microsoft")));
    }

    // --- Vendor folders ---

    [Fact]
    public void Vendor_SubfoldersAreEvaluated_TheVendorFolderIsNot()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Local\Contoso\ProductA", "a.dat", Old)
            .File(@"AppData\Local\Contoso\SuiteTwo", "b.dat", Old)
            .Build();

        LeftoverMap map = ClassifyAll(root, Snapshot(Registry("Suite Two", publisher: "Contoso Ltd")));

        Assert.Null(map.Get(RuleTestHelper.Find(root, @"AppData\Local\Contoso")));
        AssertLeftover(map.Get(RuleTestHelper.Find(root, @"AppData\Local\Contoso\ProductA")), LeftoverConfidence.Medium);
        Assert.Equal(LeftoverVerdictKind.InUse, map.Get(RuleTestHelper.Find(root, @"AppData\Local\Contoso\SuiteTwo"))!.Kind);
    }

    // --- Dot folders ---

    [Fact]
    public void DotFolder_WithoutEvidenceOrRule_IsRatedLowAtMost()
    {
        LeftoverVerdict? verdict = Classify(new TreeBuilder().File(".footool", "a.dat", Old), ".footool");

        AssertLeftover(verdict, LeftoverConfidence.Low);
        Assert.Contains("rated Low at most", verdict!.Explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void DotFolder_MatchingEvidence_IsInUse()
    {
        LeftoverVerdict? verdict = Classify(new TreeBuilder().File(".foosoft", "a.dat", Old), ".foosoft", Snapshot(Registry("FooSoft")));

        Assert.Equal(LeftoverVerdictKind.InUse, verdict!.Kind);
    }

    [Fact]
    public void DotFolder_WithOwnershipRuleThatRequiresInstallation_IsHigh()
    {
        LeftoverVerdict? verdict = Classify(
            new TreeBuilder().File(".foosoft", "a.dat", Recent),
            ".foosoft",
            Unrelated,
            RuleTestHelper.Owner("foo-owner", @"{Profile}\.foosoft", "FooSoft Studio"));

        AssertLeftover(verdict, LeftoverConfidence.High);
    }

    // --- Not candidates ---

    [Fact]
    public void NonCandidates_HaveNoVerdict()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\Prot", "a.dat", Old)
            .File(@"AppData\Roaming\CacheFolder", "a.dat", Old)
            .File(@"AppData\Roaming\Normal", "a.dat", Old)
            .File("Documents", "a.dat", Old)
            .File(@"Work\Project", "a.dat", Old)
            .Build();

        LeftoverMap map = ClassifyAll(
            root,
            Unrelated,
            RuleTestHelper.Protected("prot-rule", @"{RoamingAppData}\Prot", "subtree"),
            RuleTestHelper.Cache("cache-rule", @"{RoamingAppData}\CacheFolder"));

        Assert.Null(map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\Prot")));
        Assert.Null(map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\CacheFolder")));
        Assert.Null(map.Get(RuleTestHelper.Find(root, "Documents")));
        Assert.Null(map.Get(RuleTestHelper.Find(root, "Work")));
        Assert.NotNull(map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\Normal")));
        Assert.Equal(1, map.Count);
    }

    // --- Missing or incomplete input ---

    [Fact]
    public void Classify_WithoutEvidence_GivesAnEmptyMap()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\FooSoft", "a.dat", Old).Build();

        Assert.Equal(0, ClassifyAll(root, evidence: null).Count);
    }

    [Fact]
    public void Classify_WithoutRuleMatches_GivesAnEmptyMap()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\FooSoft", "a.dat", Old).Build();
        var classifier = new LeftoverClassifier(new FixedTimeProvider(new DateTimeOffset(Now)));

        Assert.Equal(0, classifier.Classify(root, Unrelated, null, TestContext.Current.CancellationToken).Count);
    }

    [Fact]
    public void Classify_EvidenceWithAFailedSource_NeverProducesALeftover_ButStillFindsInUse()
    {
        FolderNode root = new TreeBuilder()
            .File(@"AppData\Roaming\FooSoft", "a.dat", Old)
            .File(@"AppData\Local\Packages\Contoso.Gone_abc123", "a.dat", Old)
            .File(@"AppData\Roaming\BarSoft", "a.dat", Old)
            .Build();

        LeftoverMap map = ClassifyAll(root, SnapshotWithError(Registry("BarSoft")));

        LeftoverVerdict fooSoft = map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\FooSoft"))!;
        Assert.Equal(LeftoverVerdictKind.Unknown, fooSoft.Kind);
        Assert.Contains("could not be read completely", fooSoft.Explanation, StringComparison.Ordinal);
        Assert.Equal(LeftoverVerdictKind.Unknown, map.Get(RuleTestHelper.Find(root, @"AppData\Local\Packages\Contoso.Gone_abc123"))!.Kind);
        Assert.Equal(LeftoverVerdictKind.InUse, map.Get(RuleTestHelper.Find(root, @"AppData\Roaming\BarSoft"))!.Kind);
    }

    [Fact]
    public void Classify_Cancelled_Throws()
    {
        FolderNode root = new TreeBuilder().File(@"AppData\Roaming\FooSoft", "a.dat", Old).Build();
        RuleMatchMap matches = new RuleMatcher(RuleSet.Empty).Match(root, TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var classifier = new LeftoverClassifier(new FixedTimeProvider(new DateTimeOffset(Now)));

        Assert.Throws<OperationCanceledException>(() => classifier.Classify(root, Unrelated, matches, cancellation.Token));
    }
}

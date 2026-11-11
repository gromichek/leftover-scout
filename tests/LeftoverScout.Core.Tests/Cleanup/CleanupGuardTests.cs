using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Cleanup;

public class CleanupGuardTests
{
    private const string Item1 = Root + @"\AppData\Roaming\OldApp";
    private static readonly DateTime ScanTime = new(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FakePathProbe _probe = new();
    private readonly FakeDirectoryReader _reader = new();
    private readonly ListLogger _logger = new();

    private sealed class FakePersonalFolders : IPersonalFolderProvider
    {
        public IReadOnlyList<string> GetPersonalFolderPaths() => [Root + @"\Documents"];
    }

    private sealed class ListLogger : ILogger<CleanupGuard>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private CleanupGuard Guard(RuleSet? rules = null, ILogger<CleanupGuard>? logger = null) =>
        new(rules ?? RuleSet.Empty, _probe, _reader, new FakePersonalFolders(), [@"C:\Tools\LeftoverScout"], logger ?? NullLogger<CleanupGuard>.Instance);

    private static CleanupItem Item(string path, LeftoverConfidence? confidence = LeftoverConfidence.High, DateTime? newest = null) =>
        new(path, FindingKind.Leftover, confidence, 1000, newest ?? ScanTime);

    private static CleanupRequest Request(params CleanupItem[] items) =>
        new(new ScanTarget(Root, isCurrentUser: true), items);

    private GuardResult CheckOne(CleanupItem item, RuleSet? rules = null) =>
        Assert.Single(Guard(rules).Check(Request(item)).Items).Result;

    private static GuardWarningCode[] WarningCodes(GuardResult result) => [.. result.Warnings.Select(warning => warning.Code)];

    private static BlockReasonCode[] Codes(GuardResult result) => [.. result.BlockReasons.Select(reason => reason.Code)];

    [Fact]
    public void Check_ExistingOrdinaryFolder_IsAllowedWithoutWarnings()
    {
        _probe.Folder(Item1);

        GuardResult result = CheckOne(Item(Item1));

        Assert.True(result.IsAllowed);
        Assert.Empty(result.BlockReasons);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Check_MissingFolder_IsBlocked()
    {
        Assert.Equal([BlockReasonCode.NotFound], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_FileInsteadOfFolder_IsBlocked()
    {
        _probe.Folder(Root + @"\AppData\Roaming").Set(Item1, new PathProbe(PathProbeStatus.Found, 0x20));

        Assert.Equal([BlockReasonCode.NotADirectory], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_ItemIsAJunction_IsBlocked()
    {
        _probe.Folder(Item1).Junction(Item1);

        Assert.Equal([BlockReasonCode.ReparsePoint], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_ItemIsASymbolicLink_IsBlocked()
    {
        _probe.Folder(Item1).Set(
            Item1,
            new PathProbe(PathProbeStatus.Found, EntryClassifier.AttributeDirectory | EntryClassifier.AttributeReparsePoint, EntryClassifier.ReparseTagSymlink));

        Assert.Equal([BlockReasonCode.ReparsePoint], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_JunctionInTheMiddleOfThePath_IsBlocked()
    {
        string item = Root + @"\Work\Link\Cache";
        _probe.Folder(item).Junction(Root + @"\Work\Link");

        Assert.Equal([BlockReasonCode.ReparsePointInPath], Codes(CheckOne(Item(item))));
    }

    [Fact]
    public void Check_NonSurrogateReparsePointInThePath_IsNotABlock()
    {
        string item = Root + @"\Work\Dedup\Cache";
        _probe.Folder(item).Set(
            Root + @"\Work\Dedup",
            new PathProbe(PathProbeStatus.Found, EntryClassifier.AttributeDirectory | EntryClassifier.AttributeReparsePoint, 0x80000017));

        Assert.True(CheckOne(Item(item)).IsAllowed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Check_CloudFolderNotDownloaded_IsBlockedForTheItemAndForAParent(bool onItem)
    {
        string item = Root + @"\Work\Sync\Cache";
        _probe.Folder(item);
        string cloud = onItem ? item : Root + @"\Work\Sync";
        _probe.Set(cloud, new PathProbe(PathProbeStatus.Found, EntryClassifier.AttributeDirectory | EntryClassifier.AttributeRecallOnOpen));

        Assert.Equal([BlockReasonCode.CloudNotPopulated], Codes(CheckOne(Item(item))));
    }

    [Fact]
    public void Check_AttributesCannotBeRead_IsBlocked()
    {
        _probe.Folder(Item1).Set(Item1, PathProbe.Failed);

        Assert.Equal([BlockReasonCode.CouldNotBeChecked], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_ParentAttributesCannotBeRead_IsBlocked()
    {
        _probe.Folder(Item1).Set(Root + @"\AppData", PathProbe.Failed);

        Assert.Equal([BlockReasonCode.CouldNotBeChecked], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_LongPathCannotBeResolved_IsBlocked()
    {
        _probe.Folder(Item1).LongPath(Item1, null);

        Assert.Equal([BlockReasonCode.CouldNotBeChecked], Codes(CheckOne(Item(Item1))));
    }

    [Fact]
    public void Check_ShortNameHidingAFixedRoot_IsBlockedAfterResolvingTheLongName()
    {
        string shortForm = Root + @"\APPDAT~1\Roaming";
        _probe.Folder(Root + @"\AppData\Roaming").Folder(shortForm).LongPath(shortForm, Root + @"\AppData\Roaming");

        GuardResult result = CheckOne(Item(shortForm));

        Assert.Contains(BlockReasonCode.FixedRoot, Codes(result));
    }

    [Fact]
    public void Check_ShortNameHidingAPersonalFolder_IsBlocked()
    {
        string shortForm = Root + @"\DOCUME~1\Old";
        _probe.Folder(Root + @"\Documents\Old").Folder(shortForm).LongPath(shortForm, Root + @"\Documents\Old");

        Assert.Contains(BlockReasonCode.PersonalFolder, Codes(CheckOne(Item(shortForm))));
    }

    [Fact]
    public void Check_LongPathLeavingTheProfile_IsBlocked()
    {
        string shortForm = Root + @"\Work\OLDAPP~1";
        _probe.Folder(shortForm).LongPath(shortForm, @"C:\Elsewhere\OldApp");

        Assert.Contains(BlockReasonCode.NotInsideProfile, Codes(CheckOne(Item(shortForm))));
    }

    [Fact]
    public void Check_ShortNameForAnOrdinaryFolder_IsAllowedAndUsesTheLongFormForParents()
    {
        string shortForm = Root + @"\AppData\Roaming\OLDAPP~1";
        string longForm = Root + @"\AppData\Roaming\Old Application";
        _probe.Folder(shortForm).Folder(longForm).LongPath(shortForm, longForm);

        Assert.True(CheckOne(Item(shortForm)).IsAllowed);
        Assert.Contains(longForm, _probe.Probed);
    }

    [Fact]
    public void Check_PathProblemsAreFoundWithoutTouchingTheFileSystem()
    {
        _probe.Folder(Root + @"\Documents\Old");

        var items = new[]
        {
            Item(Root + @"\Documents\Old"),
            Item(Root + @"\AppData"),
            Item(Root + @"\AppData\Roaming\..\Roaming"),
            Item(@"C:\Tools\LeftoverScout\data"),
            Item(@"\\?\C:\Users\SampleUser\X"),
        };
        CleanupGuardReport report = Guard().Check(Request(items));

        Assert.All(report.Items, entry => Assert.False(entry.Result.IsAllowed));
        Assert.Empty(_probe.Probed);
        Assert.Empty(_probe.LongPathRequests);
        Assert.Empty(_reader.ReadPaths);
    }

    [Fact]
    public void Check_ProtectedByRule_IsBlockedAfterTheFolderIsAccepted()
    {
        _probe.Folder(Root + @"\Secret\Inner");
        RuleSet rules = Load(Protected("keep", @"{Profile}\Secret", "subtree")).RuleSet;

        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(CheckOne(Item(Root + @"\Secret\Inner"), rules)));
    }

    [Fact]
    public void Check_NewerContentThanTheScan_WarnsAboutChange()
    {
        _probe.Folder(Item1);
        _reader.Folder(Item1, FileEntry("new.bin", 5, year: 2025));

        GuardResult result = CheckOne(Item(Item1));

        Assert.True(result.IsAllowed);
        Assert.Equal([GuardWarningCode.ChangedSinceScan], WarningCodes(result));
    }

    [Fact]
    public void Check_OlderContentThanTheScan_DoesNotWarn()
    {
        _probe.Folder(Item1);
        _reader.Folder(Item1, FileEntry("old.bin", 5, year: 2023));

        Assert.Empty(CheckOne(Item(Item1)).Warnings);
    }

    [Fact]
    public void Check_ScanSawNoFilesButFilesExistNow_WarnsAboutChange()
    {
        _probe.Folder(Item1);
        _reader.Folder(Item1, FileEntry("new.bin", 5, year: 2023));

        CleanupItem item = new(Item1, FindingKind.Cache, null, 0, null);

        Assert.Equal([GuardWarningCode.ChangedSinceScan], WarningCodes(CheckOne(item)));
    }

    [Fact]
    public void Check_ScanSawNoFilesAndFolderHasOnlySubfolders_DoesNotWarn()
    {
        _probe.Folder(Item1);
        _reader.Folder(Item1, DirEntry("sub"));

        CleanupItem item = new(Item1, FindingKind.Cache, null, 0, null);

        Assert.Empty(CheckOne(item).Warnings);
    }

    [Theory]
    [InlineData(LeftoverConfidence.Low, GuardWarningCode.LowConfidence)]
    [InlineData(LeftoverConfidence.Medium, GuardWarningCode.MediumConfidence)]
    public void Check_LowOrMediumConfidence_Warns(LeftoverConfidence confidence, GuardWarningCode expected)
    {
        _probe.Folder(Item1);

        GuardResult result = CheckOne(Item(Item1, confidence));

        Assert.True(result.IsAllowed);
        Assert.Equal([expected], WarningCodes(result));
    }

    [Fact]
    public void Check_HighConfidenceAndCaches_DoNotWarn()
    {
        _probe.Folder(Item1);

        Assert.Empty(CheckOne(Item(Item1, LeftoverConfidence.High)).Warnings);
        Assert.Empty(CheckOne(Item(Item1, confidence: null)).Warnings);
    }

    [Fact]
    public void Check_BlockedItemWithWarningInputs_StaysBlockedAndHasNoWarnings()
    {
        // Low confidence and new content must not turn a blocked folder into an allowed one.
        _reader.Folder(Item1, FileEntry("new.bin", 5, year: 2025));

        GuardResult result = CheckOne(Item(Item1, LeftoverConfidence.Low));

        Assert.False(result.IsAllowed);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Check_DuplicatesAndNestedItems_AreMergedBeforeChecking()
    {
        string inner = Item1 + @"\Cache";
        _probe.Folder(inner);

        CleanupGuardReport report = Guard().Check(Request(Item(inner), Item(Item1), Item(Item1.ToUpperInvariant())));

        GuardItemResult single = Assert.Single(report.Items);
        Assert.Equal(Item1, single.Item.FullPath);
        Assert.True(single.Result.IsAllowed);
        Assert.Equal(2, report.Reduction.Merged.Count);
    }

    [Fact]
    public void Check_MixedRequest_ReturnsOneResultPerRemainingItem()
    {
        _probe.Folder(Item1);

        CleanupGuardReport report = Guard().Check(Request(Item(Item1), Item(Root + @"\Documents")));

        Assert.Equal([true, false], report.Items.Select(entry => entry.Result.IsAllowed).ToArray());
    }

    [Fact]
    public void Check_Logging_WritesCountsAndNoPaths()
    {
        _probe.Folder(Item1);

        Guard(logger: _logger).Check(Request(Item(Item1), Item(Root + @"\Documents"), Item(Root + @"\Missing")));

        string message = Assert.Single(_logger.Messages);
        Assert.Contains("3 items", message, StringComparison.Ordinal);
        Assert.Contains("1 allowed", message, StringComparison.Ordinal);
        Assert.Contains("PersonalFolder=1", message, StringComparison.Ordinal);
        Assert.DoesNotContain("SampleUser", message, StringComparison.Ordinal);
        Assert.DoesNotContain("OldApp", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_EmptyRequest_ReturnsEmptyReport()
    {
        CleanupGuardReport report = Guard().Check(Request());

        Assert.Empty(report.Items);
        Assert.Empty(report.Reduction.Items);
    }
}

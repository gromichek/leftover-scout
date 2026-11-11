using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Windows.Cleanup;
using LeftoverScout.Windows.Scanning;
using LeftoverScout.Windows.Tests.Scanning;
using Microsoft.Extensions.Logging.Abstractions;

namespace LeftoverScout.Windows.Tests.Cleanup;

/// <summary>
/// The real cleanup service on the real file system, in temporary folders only. The temp folder acts as the profile root;
/// nothing outside the temp folders created by a test is touched. Recycle Bin tests put a small fictional folder into the
/// bin of the machine that runs them.
/// </summary>
public class CleanupServiceWindowsTests
{
    private readonly WindowsPathProbe _probe = new();

    private sealed class NoPersonalFolders : IPersonalFolderProvider
    {
        public IReadOnlyList<string> GetPersonalFolderPaths() => [];
    }

    private string ProfileOf(WindowsTempFolder temp) =>
        _probe.GetLongPath(temp.Path) ?? throw new InvalidOperationException("The temp folder has no long path.");

    private CleanupService CreateService()
    {
        var reader = new Win32DirectoryReader();
        var guard = new CleanupGuard(RuleSet.Empty, _probe, reader, new NoPersonalFolders(), [], NullLogger<CleanupGuard>.Instance);
        var remover = new FolderRemover(new ShellEntryRemover(_probe), reader);
        return new CleanupService(guard, remover, new WindowsRecycleBinSettingsReader(), reader, TimeProvider.System, NullLogger<CleanupService>.Instance);
    }

    private async Task<CleanupItemResult> CleanAsync(string profile, string folder, CleanupMode mode)
    {
        var item = new CleanupItem(folder, FindingKind.Leftover, LeftoverConfidence.High, 1000, null);
        CleanupSessionResult session = await CreateService().RunAsync(
            new CleanupRequest(new ScanTarget(profile, isCurrentUser: false), [item]),
            mode,
            null,
            TestContext.Current.CancellationToken);
        return Assert.Single(session.Items);
    }

    private static string MakeNestedFolder(string profile)
    {
        string folder = Path.Combine(profile, "FictionalOldApp");
        Directory.CreateDirectory(Path.Combine(folder, "cache", "deep"));
        File.WriteAllBytes(Path.Combine(folder, "settings.bin"), new byte[100]);
        File.WriteAllBytes(Path.Combine(folder, "cache", "a.bin"), new byte[100]);
        File.WriteAllBytes(Path.Combine(folder, "cache", "deep", "b.bin"), new byte[100]);
        return folder;
    }

    [Fact]
    public async Task Run_PermanentMode_RemovesNestedFolder()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = MakeNestedFolder(profile);

        CleanupItemResult result = await CleanAsync(profile, folder, CleanupMode.Permanent);

        Assert.Equal(CleanupOutcome.Removed, result.Outcome);
        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public async Task Run_RecycleBinMode_SmallFolder_IsRemovedAndReallyRecycled()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = MakeNestedFolder(profile);

        CleanupItemResult result = await CleanAsync(profile, folder, CleanupMode.RecycleBin);

        Assert.True(result.Outcome == CleanupOutcome.Removed, $"{result.Outcome}: {result.Reason}");
        Assert.False(result.DeletedPermanentlyUnexpectedly, "Layer 3: the folder was deleted permanently instead of being recycled.");
        Assert.False(Directory.Exists(folder));
    }

    [Theory]
    [InlineData(CleanupMode.Permanent)]
    [InlineData(CleanupMode.RecycleBin)]
    public async Task Run_JunctionInsideFolder_TargetSurvives(CleanupMode mode)
    {
        using var temp = new WindowsTempFolder();
        using var targetTemp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = MakeNestedFolder(profile);
        string targetFile = Path.Combine(targetTemp.Path, "precious.txt");
        File.WriteAllText(targetFile, "keep me");
        string link = Path.Combine(folder, "cache", "link-to-other");
        Assert.True(JunctionHelper.TryCreate(link, targetTemp.Path), "The test could not create a junction.");

        CleanupItemResult result = await CleanAsync(profile, folder, mode);

        Assert.True(File.Exists(targetFile), "The junction target was damaged.");
        Assert.Equal("keep me", File.ReadAllText(targetFile));
        Assert.True(Directory.Exists(targetTemp.Path));
        Assert.NotEqual(CleanupOutcome.Blocked, result.Outcome);
        Assert.False(result.DeletedPermanentlyUnexpectedly);
    }

    [Theory]
    [InlineData(CleanupMode.Permanent)]
    [InlineData(CleanupMode.RecycleBin)]
    public async Task Run_FileInUse_IsPartlyRemovedAndTheRestIsGone(CleanupMode mode)
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = MakeNestedFolder(profile);
        string lockedFile = Path.Combine(folder, "cache", "deep", "b.bin");

        CleanupItemResult result;
        using (new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            result = await CleanAsync(profile, folder, mode);

            Assert.True(File.Exists(lockedFile));
        }

        Assert.True(result.Outcome == CleanupOutcome.PartlyRemoved, $"{result.Outcome}: {result.Reason}");
        Assert.True(result.ItemsLeft >= 1);
        Assert.False(File.Exists(Path.Combine(folder, "settings.bin")));
        Assert.False(File.Exists(Path.Combine(folder, "cache", "a.bin")));
    }

    [Fact]
    public async Task Run_FolderOutsideTheProfile_IsBlockedAndUntouched()
    {
        using var temp = new WindowsTempFolder();
        string root = ProfileOf(temp);
        string profile = Path.Combine(root, "FakeProfile");
        Directory.CreateDirectory(profile);
        string outside = Path.Combine(root, "Outside", "FictionalOldApp");
        Directory.CreateDirectory(outside);
        string file = Path.Combine(outside, "keep.bin");
        File.WriteAllBytes(file, new byte[10]);

        CleanupItemResult result = await CleanAsync(profile, outside, CleanupMode.Permanent);

        Assert.Equal(CleanupOutcome.Blocked, result.Outcome);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public async Task Run_MissingFolder_IsBlockedByTheGuard()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);

        CleanupItemResult result = await CleanAsync(profile, Path.Combine(profile, "DoesNotExist"), CleanupMode.Permanent);

        Assert.Equal(CleanupOutcome.Blocked, result.Outcome);
    }
}

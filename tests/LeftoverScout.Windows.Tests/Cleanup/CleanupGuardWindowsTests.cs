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

/// <summary>The guard with the real Windows probe, in temporary folders that act as the profile. Nothing is deleted.</summary>
public class CleanupGuardWindowsTests
{
    private readonly WindowsPathProbe _probe = new();

    private sealed class NoPersonalFolders : IPersonalFolderProvider
    {
        public IReadOnlyList<string> GetPersonalFolderPaths() => [];
    }

    // The temp folder acts as the profile root. Its long form is used, because the temp path can contain 8.3 names.
    private string ProfileOf(WindowsTempFolder temp) =>
        _probe.GetLongPath(temp.Path) ?? throw new InvalidOperationException("The temp folder has no long path.");

    private static RuleSet Protected(string path)
    {
        string json = $$"""{ "schemaVersion": 1, "rules": [ { "id": "keep", "kind": "protected", "paths": ["{{path.Replace("\\", "\\\\")}}"], "scope": "subtree", "description": "d", "source": "s" } ] }""";
        RuleLoadResult result = RuleLoader.Load([new RuleFile("test.json", json)]);
        Assert.Empty(result.Errors);
        return result.RuleSet;
    }

    private GuardResult Check(string profile, string path, RuleSet? rules = null)
    {
        var guard = new CleanupGuard(
            rules ?? RuleSet.Empty,
            _probe,
            new Win32DirectoryReader(),
            new NoPersonalFolders(),
            [],
            NullLogger<CleanupGuard>.Instance);
        var item = new CleanupItem(path, FindingKind.Leftover, LeftoverConfidence.High, 0, DateTime.UtcNow);
        CleanupGuardReport report = guard.Check(new CleanupRequest(new ScanTarget(profile, isCurrentUser: false), [item]));
        return Assert.Single(report.Items).Result;
    }

    private static BlockReasonCode[] Codes(GuardResult result) => [.. result.BlockReasons.Select(reason => reason.Code)];

    [Fact]
    public void Check_OrdinaryFolder_IsAllowed()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = Path.Combine(profile, "OldApp", "Cache");
        Directory.CreateDirectory(folder);

        GuardResult result = Check(profile, folder);

        Assert.True(result.IsAllowed, string.Join("; ", result.BlockReasons.Select(r => r.Text)));
    }

    [Fact]
    public void Check_FolderWithNewerContentThanTheScan_WarnsAboutChange()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = Path.Combine(profile, "OldApp");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "new.bin"), new byte[5]);

        var guard = new CleanupGuard(RuleSet.Empty, _probe, new Win32DirectoryReader(), new NoPersonalFolders(), [], NullLogger<CleanupGuard>.Instance);
        var item = new CleanupItem(folder, FindingKind.Cache, null, 0, DateTime.UtcNow.AddDays(-30));
        GuardResult result = Assert.Single(guard.Check(new CleanupRequest(new ScanTarget(profile, false), [item])).Items).Result;

        Assert.True(result.IsAllowed);
        Assert.Equal(GuardWarningCode.ChangedSinceScan, Assert.Single(result.Warnings).Code);
    }

    [Fact]
    public void Check_MissingFolder_IsBlocked()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);

        Assert.Equal([BlockReasonCode.NotFound], Codes(Check(profile, Path.Combine(profile, "missing"))));
    }

    [Fact]
    public void Check_FileInsteadOfFolder_IsBlocked()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string file = Path.Combine(profile, "data.bin");
        File.WriteAllBytes(file, new byte[5]);

        Assert.Equal([BlockReasonCode.NotADirectory], Codes(Check(profile, file)));
    }

    [Fact]
    public void Check_JunctionAsTheItem_IsBlocked()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string target = Path.Combine(profile, "target");
        string link = Path.Combine(profile, "link");
        Directory.CreateDirectory(target);
        if (!JunctionHelper.TryCreate(link, target))
        {
            Assert.Skip("A directory junction could not be created in this environment.");
        }

        try
        {
            Assert.Equal([BlockReasonCode.ReparsePoint], Codes(Check(profile, link)));
        }
        finally
        {
            JunctionHelper.Remove(link);
        }
    }

    [Fact]
    public void Check_JunctionInTheMiddleOfThePath_IsBlocked()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string target = Path.Combine(profile, "target");
        string link = Path.Combine(profile, "link");
        Directory.CreateDirectory(Path.Combine(target, "Cache"));
        if (!JunctionHelper.TryCreate(link, target))
        {
            Assert.Skip("A directory junction could not be created in this environment.");
        }

        try
        {
            Assert.Equal([BlockReasonCode.ReparsePointInPath], Codes(Check(profile, Path.Combine(link, "Cache"))));
        }
        finally
        {
            JunctionHelper.Remove(link);
        }
    }

    [Fact]
    public void Check_PathLongerThan260Characters_IsAllowed()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string deep = profile;
        for (int i = 0; i < 6; i++)
        {
            deep = Path.Combine(deep, new string((char)('a' + i), 50));
        }

        Directory.CreateDirectory(deep);
        Assert.True(deep.Length > 260);

        GuardResult result = Check(profile, deep);

        Assert.True(result.IsAllowed, string.Join("; ", result.BlockReasons.Select(r => r.Text)));
    }

    [Fact]
    public void Check_ShortNameOfAProtectedFolder_IsBlocked()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string protectedFolder = Path.Combine(profile, "Protected Folder Name");
        Directory.CreateDirectory(Path.Combine(protectedFolder, "Inner"));
        string? shortLeaf = ShortPathHelper.GetShortLeafName(protectedFolder);
        if (shortLeaf is null)
        {
            Assert.Skip("8.3 short names are disabled on this volume.");
        }

        RuleSet rules = Protected(@"{Profile}\Protected Folder Name");
        string viaShortName = Path.Combine(profile, shortLeaf, "Inner");

        // The text of the path does not match the rule, the long form does.
        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(Check(profile, viaShortName, rules)));
        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(Check(profile, Path.Combine(protectedFolder, "Inner"), rules)));
    }

    [Fact]
    public void Check_ShortNameOfAnOrdinaryFolder_IsAllowed()
    {
        using var temp = new WindowsTempFolder();
        string profile = ProfileOf(temp);
        string folder = Path.Combine(profile, "An Ordinary Long Folder");
        Directory.CreateDirectory(folder);
        string? shortLeaf = ShortPathHelper.GetShortLeafName(folder);
        if (shortLeaf is null)
        {
            Assert.Skip("8.3 short names are disabled on this volume.");
        }

        GuardResult result = Check(profile, Path.Combine(profile, shortLeaf));

        Assert.True(result.IsAllowed, string.Join("; ", result.BlockReasons.Select(r => r.Text)));
    }
}

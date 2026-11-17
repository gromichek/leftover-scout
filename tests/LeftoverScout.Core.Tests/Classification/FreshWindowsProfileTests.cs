using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Rules;
using static LeftoverScout.Core.Tests.Classification.Evidences;

namespace LeftoverScout.Core.Tests.Classification;

/// <summary>
/// A synthetic "fresh Windows 11" profile (names only, fictional user) run through the shipped rules and the classifier.
/// Every folder is older than six months, which is the worst case. No folder may be rated a leftover.
/// </summary>
public class FreshWindowsProfileTests
{
    private static readonly DateTime Now = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Old = new(2023, 5, 20, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Folders of the fixture, relative to the profile root. Extend this list for other profiles.</summary>
    private static readonly string[] FreshWindows11Folders =
    [
        @"AppData\Local\Comms",
        @"AppData\Local\ConnectedDevicesPlatform",
        @"AppData\Local\CrashDumps",
        @"AppData\Local\D3DSCache",
        @"AppData\Local\IsolatedStorage",
        @"AppData\Local\Microsoft\Windows",
        @"AppData\Local\Microsoft\Edge",
        @"AppData\Local\Microsoft\OneDrive",
        @"AppData\Local\Package Cache",
        @"AppData\Local\Packages\Microsoft.WindowsCalculator_8wekyb3d8bbwe",
        @"AppData\Local\Packages\MicrosoftWindows.Client.CBS_cw5n1h2txyewy",
        @"AppData\Local\Packages\windows.immersivecontrolpanel_cw5n1h2txyewy",
        @"AppData\Local\Packages\MSTeams_8wekyb3d8bbwe",
        @"AppData\Local\PeerDistRepub",
        @"AppData\Local\PlaceholderTileLogoFolder",
        @"AppData\Local\Programs",
        @"AppData\Local\Publishers",
        @"AppData\Local\Temp",
        @"AppData\Local\VirtualStore",
        @"AppData\Roaming\Microsoft",
        @"AppData\Roaming\npm",
        @"AppData\Roaming\NuGet",
        @"AppData\LocalLow\Microsoft",
        ".ssh",
        ".gnupg",
        ".aws",
        ".azure",
        ".kube",
        ".docker",
        ".android",
    ];

    private static readonly EvidenceSnapshot FreshWindows11Evidence = Snapshot(
        Registry("Microsoft Edge", "Microsoft Corporation"),
        Registry("Microsoft OneDrive", "Microsoft Corporation"),
        Store("Microsoft Windows Calculator", "Microsoft.WindowsCalculator_8wekyb3d8bbwe", "Microsoft Corporation"),
        Store("Windows Client CBS", "MicrosoftWindows.Client.CBS_cw5n1h2txyewy", "Microsoft Corporation"),
        Store("Immersive Control Panel", "windows.immersivecontrolpanel_cw5n1h2txyewy", "Microsoft Corporation"),
        Store("Microsoft Teams", "MSTeams_8wekyb3d8bbwe", "Microsoft Corporation"));

    private static TreeBuilder Tree(IEnumerable<string> folders)
    {
        var tree = new TreeBuilder();
        foreach (string folder in folders)
        {
            tree.File(folder, "data.dat", Old);
        }

        return tree;
    }

    [Fact]
    public void FreshWindows11Profile_ProducesNoLeftoverVerdict()
    {
        FolderNode root = Tree(FreshWindows11Folders).Build();
        RuleSet rules = RuleLoader.Load(EmbeddedRuleFiles.Read()).RuleSet;
        RuleMatchMap matches = new RuleMatcher(rules).Match(root, TestContext.Current.CancellationToken);
        LeftoverMap map = new LeftoverClassifier(new FixedTimeProvider(new DateTimeOffset(Now)))
            .Classify(root, FreshWindows11Evidence, matches, TestContext.Current.CancellationToken);

        var offenders = new List<string>();
        foreach (string folder in FreshWindows11Folders)
        {
            LeftoverVerdict? verdict = map.Get(RuleTestHelper.Find(root, folder));
            if (verdict is { Kind: LeftoverVerdictKind.Leftover })
            {
                offenders.Add($"{folder}: Leftover {verdict.Confidence} ({verdict.Explanation})");
            }
        }

        Assert.True(offenders.Count == 0, "Folders rated as leftovers: " + string.Join("; ", offenders));
    }
}

using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Rules;
using LeftoverScout.Core.Scanning;
using static LeftoverScout.Core.Tests.Rules.RuleTestHelper;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Cleanup;

public class CleanupPathCheckerTests
{
    private const string Profile = Root; // C:\Users\SampleUser

    private static CleanupPathChecker Checker(RuleSet? rules = null, string[]? personal = null, string[]? app = null) =>
        new(new ScanTarget(Profile, isCurrentUser: true), new RuleMatcher(rules ?? RuleSet.Empty), personal ?? [], app ?? []);

    private static RuleSet Rules(params string[] ruleJson)
    {
        RuleLoadResult result = Load(ruleJson);
        Assert.Empty(result.Errors);
        return result.RuleSet;
    }

    private static BlockReasonCode[] Codes(CleanupPathChecker checker, string path) =>
        [.. checker.Check(path).Select(reason => reason.Code).Distinct()];

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"Users\SampleUser\OldApp")]
    [InlineData(@"\Users\SampleUser\OldApp")]
    [InlineData(@"C:Users\SampleUser\OldApp")]
    [InlineData(@"C:\Users\SampleUser\OldApp\..\Cache")]
    [InlineData(@"C:\Users\SampleUser\.\OldApp")]
    [InlineData(@"C:\Users\SampleUser\OldApp\..")]
    [InlineData(@"C:\Users\SampleUser\Old*")]
    [InlineData(@"C:\Users\SampleUser\*")]
    [InlineData(@"C:\Users\SampleUser\Old?")]
    [InlineData(@"C:\Users\SampleUser\Old.")]
    [InlineData(@"C:\Users\SampleUser\Old ")]
    [InlineData(@"C:\Users\SampleUser\Old\..\..\..\Windows")]
    [InlineData(@"C:\Users\SampleUser\Old:stream")]
    [InlineData(@"C:\Users\SampleUser\\OldApp")]
    [InlineData(@"\\server")]
    public void Check_NotAPlainFullyQualifiedPath_IsInvalid(string path)
    {
        Assert.Equal([BlockReasonCode.InvalidPath], Codes(Checker(), path));
    }

    [Fact]
    public void Check_NullPath_IsInvalid()
    {
        Assert.Equal([BlockReasonCode.InvalidPath], Codes(Checker(), null!));
    }

    [Theory]
    [InlineData(@"\\?\C:\Users\SampleUser\OldApp")]
    [InlineData(@"//?/C:/Users/SampleUser/OldApp")]
    [InlineData(@"\\.\C:\Users\SampleUser\OldApp")]
    [InlineData(@"\??\C:\Users\SampleUser\OldApp")]
    [InlineData(@"C:\Users\SampleUser\NUL")]
    [InlineData(@"C:\Users\SampleUser\com1.txt")]
    [InlineData(@"C:\Users\SampleUser\OldApp\LPT3")]
    public void Check_DeviceOrExtendedPath_IsBlocked(string path)
    {
        Assert.Equal([BlockReasonCode.DevicePath], Codes(Checker(), path));
    }

    [Theory]
    [InlineData(@"C:\Users\SampleUser2\OldApp")]
    [InlineData(@"C:\Users\SampleUser2")]
    [InlineData(@"C:\Users\SampleUse\OldApp")]
    [InlineData(@"C:\Users")]
    [InlineData(@"C:\")]
    [InlineData(@"D:\Users\SampleUser\OldApp")]
    [InlineData(@"C:\Users\Other\OldApp")]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"\\server\share\Users\SampleUser\OldApp")]
    public void Check_OutsideTheProfile_IsBlocked(string path)
    {
        Assert.Equal([BlockReasonCode.NotInsideProfile], Codes(Checker(), path));
    }

    [Theory]
    [InlineData(@"C:\Users\SampleUser")]
    [InlineData(@"C:\Users\SampleUser\")]
    [InlineData(@"c:/users/sampleuser")]
    public void Check_ProfileRootItself_IsBlocked(string path)
    {
        Assert.Equal([BlockReasonCode.FixedRoot], Codes(Checker(), path));
    }

    [Theory]
    [InlineData(@"C:\Users\SampleUser\AppData")]
    [InlineData(@"C:\Users\SampleUser\AppData\Roaming")]
    [InlineData(@"C:\Users\SampleUser\AppData\Local")]
    [InlineData(@"C:\Users\SampleUser\AppData\LocalLow")]
    [InlineData(@"C:\Users\SampleUser\APPDATA\local\")]
    [InlineData(@"C:/Users/SampleUser/AppData/Roaming")]
    [InlineData(@"c:\users\sampleuser\appdata\locallow")]
    public void Check_FixedRoot_IsBlockedEvenWithoutRules(string path)
    {
        Assert.Contains(BlockReasonCode.FixedRoot, Codes(Checker(RuleSet.Empty), path));
    }

    [Theory]
    [InlineData(@"AppData\Roaming\Microsoft")]
    [InlineData(@"AppData\Local\Microsoft")]
    [InlineData(@"AppData\Local\Packages")]
    [InlineData(@"AppData\Local\Programs")]
    [InlineData(@"AppData\Roaming\Microsoft\Protect")]
    [InlineData(@"AppData\Roaming\Microsoft\Protect\S-1-5-21-1\key")]
    [InlineData(@"AppData\Roaming\Microsoft\Credentials")]
    [InlineData(@"AppData\Local\Microsoft\Credentials\x")]
    [InlineData(@"AppData\Roaming\Microsoft\SystemCertificates\My")]
    [InlineData(@"AppData\Roaming\Microsoft\Crypto")]
    [InlineData(@"AppData\Local\Microsoft\Vault")]
    [InlineData(@"AppData\Roaming\Microsoft\Windows\Start Menu")]
    [InlineData(@"AppData\Roaming\Microsoft\Windows\Start Menu\Programs")]
    [InlineData(@"AppData\Roaming\Microsoft\Windows")] // contains Start Menu
    [InlineData(@"AppData\Local\Microsoft\WindowsApps")]
    [InlineData(@"appdata\LOCAL\microsoft\windowsapps\x")]
    public void Check_DenyList_BlocksEvenWhenTheRuleSetIsEmpty(string relative)
    {
        Assert.Contains(BlockReasonCode.DenyList, Codes(Checker(RuleSet.Empty), Profile + "\\" + relative));
    }

    [Theory]
    [InlineData(@"AppData\Local\Packages\Contoso.App_abc")]
    [InlineData(@"AppData\Local\Programs\Contoso")]
    [InlineData(@"AppData\Roaming\Microsoft\Teams")]
    [InlineData(@"AppData\Roaming\Microsoft\Windows\Recent")]
    [InlineData(@"AppData\Local\Microsoft\Windows\INetCache")]
    [InlineData(@"AppData\Roaming\OldApp")]
    [InlineData(@"AppData\Local\OldApp\Cache")]
    [InlineData(@"AppData\LocalLow\OldGame")]
    [InlineData(@".gradle")]
    [InlineData(@"OldTool")]
    public void Check_OrdinaryFolder_IsAllowed(string relative)
    {
        Assert.Empty(Checker(RuleSet.Empty).Check(Profile + "\\" + relative));
    }

    [Fact]
    public void Check_MixedSlashesTrailingSeparatorAndCase_AreTreatedLikeTheNormalForm()
    {
        CleanupPathChecker checker = Checker();

        Assert.Empty(checker.Check(@"C:/Users/SampleUser\AppData/Roaming\OldApp"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\AppData\Roaming\OldApp\"));
        Assert.Empty(checker.Check(@"c:\USERS\sampleuser\appdata\ROAMING\oldapp"));
    }

    [Theory]
    [InlineData(@"Documents")]
    [InlineData(@"documents")]
    [InlineData(@"Documents\Old")]
    [InlineData(@"Documents\a\b\c")]
    [InlineData(@"Desktop")]
    [InlineData(@"Downloads\installers")]
    [InlineData(@"Pictures")]
    [InlineData(@"Videos")]
    [InlineData(@"Music\x")]
    [InlineData(@"Saved Games")]
    [InlineData(@"3D Objects")]
    [InlineData(@"OneDrive")]
    [InlineData(@"OneDrive - Contoso")]
    [InlineData(@"OneDrive - Contoso\Sub\Deep")]
    [InlineData(@"OneDriveBackup")]
    public void Check_PersonalByName_IsBlockedEvenWithoutRules(string relative)
    {
        Assert.Contains(BlockReasonCode.PersonalFolder, Codes(Checker(RuleSet.Empty), Profile + "\\" + relative));
    }

    [Theory]
    [InlineData(@"Documents2")]
    [InlineData(@"AppData\Roaming\Documents")]
    [InlineData(@"Work\Documents")]
    public void Check_PersonalNameNotDirectlyInTheProfileRoot_IsAllowed(string relative)
    {
        Assert.Empty(Checker(RuleSet.Empty).Check(Profile + "\\" + relative));
    }

    [Fact]
    public void Check_KnownFolderPaths_AreBlockedWithTheirAncestorsAndDescendants()
    {
        CleanupPathChecker checker = Checker(
            personal: [@"C:\Users\SampleUser\Saved Stuff", @"C:\Users\SampleUser\Work\Projects", @"D:\Redirected\Docs", "not a path"]);

        Assert.Contains(BlockReasonCode.PersonalFolder, Codes(checker, @"C:\Users\SampleUser\Saved Stuff"));
        Assert.Contains(BlockReasonCode.PersonalFolder, Codes(checker, @"C:\Users\SampleUser\saved stuff\sub"));
        Assert.Contains(BlockReasonCode.PersonalFolder, Codes(checker, @"C:\Users\SampleUser\Work\Projects\x"));

        // The folder that contains a personal folder would delete it.
        Assert.Contains(BlockReasonCode.PersonalFolder, Codes(checker, @"C:\Users\SampleUser\Work"));

        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Work\Other"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Saved Stuff2"));
    }

    [Fact]
    public void Check_AppFolders_AreBlockedWithAncestorsAndDescendants()
    {
        CleanupPathChecker checker = Checker(app: [@"C:\Users\SampleUser\Tools\LeftoverScout", @"C:\Users\SampleUser\Tools\LeftoverScout\data"]);

        Assert.Contains(BlockReasonCode.AppFolder, Codes(checker, @"C:\Users\SampleUser\Tools\LeftoverScout"));
        Assert.Contains(BlockReasonCode.AppFolder, Codes(checker, @"C:\Users\SampleUser\Tools\LeftoverScout\data"));
        Assert.Contains(BlockReasonCode.AppFolder, Codes(checker, @"C:\Users\SampleUser\Tools\LeftoverScout\data\logs"));
        Assert.Contains(BlockReasonCode.AppFolder, Codes(checker, @"C:\Users\SampleUser\Tools"));
        Assert.Contains(BlockReasonCode.AppFolder, Codes(checker, @"c:\users\sampleuser\TOOLS\leftoverscout\Data"));

        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Tools\Other"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Tools\LeftoverScout2"));
    }

    [Fact]
    public void Check_AppFolderOutsideTheProfile_DoesNotBlockProfileFolders()
    {
        CleanupPathChecker checker = Checker(app: [@"C:\Program Files\LeftoverScout"]);

        Assert.Empty(checker.Check(@"C:\Users\SampleUser\AppData\Roaming\OldApp"));
    }

    [Fact]
    public void Check_SubtreeProtectedRule_BlocksTheFolderAndEverythingBelow()
    {
        CleanupPathChecker checker = Checker(Rules(Protected("keep-secret", @"{Profile}\Secret", "subtree")));

        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\Secret"));
        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\secret\a\b"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Secret2"));
    }

    [Fact]
    public void Check_FolderProtectedRule_BlocksOnlyTheFolderItself()
    {
        CleanupPathChecker checker = Checker(Rules(Protected("keep-root", @"{Profile}\Keep", "folder")));

        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\Keep"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Keep\child"));
    }

    [Fact]
    public void Check_ProtectedRuleWithWildcard_BlocksMatchesAndTheirParents()
    {
        CleanupPathChecker checker = Checker(Rules(Protected("keys", @"{Profile}\Apps\*\Keys", "subtree")));

        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\Apps\One\Keys"));
        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\Apps\One\Keys\k1"));

        // A parent would delete the protected folder.
        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\Apps\One"));
        Assert.Contains(BlockReasonCode.ProtectedByRule, Codes(checker, @"C:\Users\SampleUser\Apps"));

        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Apps\One\Other"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\Other"));
    }

    [Fact]
    public void Check_ShippedProtectedRules_BlockTheirPaths()
    {
        RuleSet shipped = EmbeddedRuleLoader.Load(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        CleanupPathChecker checker = Checker(shipped);

        Assert.NotEmpty(checker.Check(@"C:\Users\SampleUser\AppData\Local\Microsoft\Vault"));
        Assert.NotEmpty(checker.Check(@"C:\Users\SampleUser\AppData\Local\Packages"));
        Assert.Empty(checker.Check(@"C:\Users\SampleUser\AppData\Roaming\SampleOldApp"));
    }

    [Fact]
    public void Check_SeveralProblems_ReportsAllReasons()
    {
        CleanupPathChecker checker = Checker(app: [@"C:\Users\SampleUser\Documents\Tool"]);

        BlockReasonCode[] codes = Codes(checker, @"C:\Users\SampleUser\Documents\Tool");

        Assert.Contains(BlockReasonCode.PersonalFolder, codes);
        Assert.Contains(BlockReasonCode.AppFolder, codes);
    }

    [Fact]
    public void Check_EveryReasonHasEnglishText()
    {
        foreach (BlockReasonCode code in Enum.GetValues<BlockReasonCode>())
        {
            Assert.False(string.IsNullOrWhiteSpace(BlockReason.Of(code).Text), code.ToString());
        }
    }
}

using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Scanning;

public class PersonalFolderMatcherTests
{
    private const string ProfileRoot = @"C:\Users\SampleUser";

    [Fact]
    public void IsPersonal_FolderInsideProfile_MatchesIgnoringCaseAndTrailingSeparator()
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, [@"C:\Users\SampleUser\Documents\"]);

        Assert.True(matcher.IsPersonal(@"c:\users\sampleuser\DOCUMENTS"));
    }

    [Fact]
    public void IsPersonal_FolderOutsideProfile_IsIgnored()
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, [@"D:\Redirected\Pictures", @"C:\Users\OtherUser\Documents"]);

        Assert.False(matcher.IsPersonal(@"D:\Redirected\Pictures"));
        Assert.False(matcher.IsPersonal(@"C:\Users\OtherUser\Documents"));
    }

    [Fact]
    public void IsPersonal_ProfileRootItselfAndInvalidPaths_AreIgnored()
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, [ProfileRoot, "", "Documents", @"C:\Users\SampleUserX\Docs"]);

        Assert.False(matcher.IsPersonal(ProfileRoot));
        Assert.False(matcher.IsPersonal(@"C:\Users\SampleUserX\Docs"));
    }

    [Fact]
    public void IsPersonal_Subfolder_IsNotMatchedOnItsOwn()
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, [@"C:\Users\SampleUser\Documents"]);

        Assert.False(matcher.IsPersonal(@"C:\Users\SampleUser\Documents\Work"));
    }
}

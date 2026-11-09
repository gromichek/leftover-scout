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

    [Theory]
    [InlineData("Documents")]
    [InlineData("desktop")]
    [InlineData("Downloads")]
    [InlineData("Pictures")]
    [InlineData("Videos")]
    [InlineData("Music")]
    [InlineData("Favorites")]
    [InlineData("Contacts")]
    [InlineData("Links")]
    [InlineData("Saved Games")]
    [InlineData("Searches")]
    [InlineData("3D Objects")]
    [InlineData("OneDrive")]
    [InlineData("OneDrive - Contoso")]
    public void IsPersonal_StandardNameInProfileRoot_IsPersonalWithoutKnownFolderPaths(string name)
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, []);

        Assert.True(matcher.IsPersonal(ProfileRoot + "\\" + name));
    }

    [Fact]
    public void IsPersonal_RedirectedDocuments_StillLeavesProfileDocumentsPersonal()
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, [@"D:\Redirected\Documents"]);

        Assert.True(matcher.IsPersonal(@"C:\Users\SampleUser\Documents"));
        Assert.False(matcher.IsPersonal(@"D:\Redirected\Documents"));
    }

    [Theory]
    [InlineData(@"C:\Users\SampleUser\Documents2")]
    [InlineData(@"C:\Users\SampleUser\My Documents")]
    [InlineData(@"C:\Users\SampleUser\AppData")]
    [InlineData(@"C:\Users\SampleUser\AppData\Local\Documents")]
    [InlineData(@"C:\Users\SampleUser\Work\Music")]
    [InlineData(@"C:\Users\SampleUser\MyOneDrive")]
    public void IsPersonal_NearMissesAndNestedNames_AreNotPersonal(string path)
    {
        var matcher = new PersonalFolderMatcher(ProfileRoot, []);

        Assert.False(matcher.IsPersonal(path));
    }
}

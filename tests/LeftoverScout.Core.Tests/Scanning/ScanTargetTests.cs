using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Scanning;

public class ScanTargetTests
{
    // Fictional path: tests must never contain real user names or touch a real profile.
    private const string SampleProfilePath = @"C:\Users\SampleUser";

    [Fact]
    public void ForCurrentUser_ReturnsFullyQualifiedPathOfCurrentUser()
    {
        var target = ScanTarget.ForCurrentUser();

        Assert.True(target.IsCurrentUser);
        Assert.True(Path.IsPathFullyQualified(target.ProfileRootPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyPath(string path)
    {
        Assert.Throws<ArgumentException>(() => new ScanTarget(path, isCurrentUser: true));
    }

    [Fact]
    public void Constructor_RejectsRelativePath()
    {
        Assert.Throws<ArgumentException>(() => new ScanTarget(@"Users\SampleUser", isCurrentUser: true));
    }

    [Fact]
    public void Constructor_TrimsTrailingSeparator()
    {
        var target = new ScanTarget(SampleProfilePath + @"\", isCurrentUser: false);

        Assert.Equal(SampleProfilePath, target.ProfileRootPath);
        Assert.False(target.IsCurrentUser);
    }

    [Fact]
    public void Targets_WithSameValues_AreEqual()
    {
        Assert.Equal(new ScanTarget(SampleProfilePath, true), new ScanTarget(SampleProfilePath, true));
    }
}

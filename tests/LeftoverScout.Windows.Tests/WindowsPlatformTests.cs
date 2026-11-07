namespace LeftoverScout.Windows.Tests;

public class WindowsPlatformTests
{
    [Fact]
    public void MinimumBuild_IsWindows10Version22H2()
    {
        Assert.Equal(19045, WindowsPlatform.MinimumBuild);
    }

    [Fact]
    public void IsSupportedOs_IsTrueOnCiRunner()
    {
        // CI runs on a current Windows image, which is always newer than the minimum build.
        Assert.True(WindowsPlatform.IsSupportedOs());
    }
}

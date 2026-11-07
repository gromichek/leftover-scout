using LeftoverScout.Windows.Shell;

namespace LeftoverScout.Windows.Tests.Shell;

public class ShellLauncherTests
{
    [Fact]
    public void BuildSelectArguments_Path_IsQuotedAfterSelectSwitch()
    {
        string? arguments = ShellLauncher.BuildSelectArguments(@"C:\Users\SampleUser\My Videos\big file.mkv");

        Assert.Equal("/select,\"C:\\Users\\SampleUser\\My Videos\\big file.mkv\"", arguments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\Users\\SampleUser\\bad\"name.txt")]
    public void BuildSelectArguments_EmptyOrContainingQuote_ReturnsNull(string path)
    {
        Assert.Null(ShellLauncher.BuildSelectArguments(path));
    }
}

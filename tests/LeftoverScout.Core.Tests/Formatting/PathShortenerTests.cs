using LeftoverScout.Core.Formatting;

namespace LeftoverScout.Core.Tests.Formatting;

public class PathShortenerTests
{
    [Fact]
    public void Shorten_ShortPath_IsUnchanged()
    {
        Assert.Equal(@"C:\Users\SampleUser", PathShortener.Shorten(@"C:\Users\SampleUser", 40));
    }

    [Fact]
    public void Shorten_LongPath_KeepsTheEndWithinMaxLength()
    {
        string shortened = PathShortener.Shorten(@"C:\Users\SampleUser\AppData\Local\Vendor\Product\Cache", 20);

        Assert.Equal(20, shortened.Length);
        Assert.StartsWith("…", shortened);
        Assert.EndsWith(@"Product\Cache", shortened);
    }

    [Fact]
    public void Shorten_MaxLengthTooSmall_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PathShortener.Shorten("abc", 1));
    }
}

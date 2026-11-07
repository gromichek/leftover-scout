using LeftoverScout.Core.Formatting;

namespace LeftoverScout.Core.Tests.Formatting;

public class DurationFormatterTests
{
    [Theory]
    [InlineData(0, "0 s")]
    [InlineData(42, "42 s")]
    [InlineData(59, "59 s")]
    [InlineData(60, "1 min 00 s")]
    [InlineData(185, "3 min 05 s")]
    [InlineData(3720, "1 h 02 min")]
    [InlineData(-4, "0 s")]
    public void Format_ReturnsReadableText(int seconds, string expected)
    {
        Assert.Equal(expected, DurationFormatter.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Format_TruncatesFractionalSeconds()
    {
        Assert.Equal("1 s", DurationFormatter.Format(TimeSpan.FromMilliseconds(1999)));
    }
}

using System.Globalization;
using LeftoverScout.Core.Formatting;

namespace LeftoverScout.Core.Tests.Formatting;

public class SizeFormatterTests
{
    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(1L, "1 B")]
    [InlineData(1023L, "1023 B")]
    [InlineData(1024L, "1.0 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1.0 MB")]
    [InlineData(1610612736L, "1.5 GB")]
    [InlineData(1099511627776L, "1.0 TB")]
    [InlineData(1048575L, "1.0 MB")] // 1023.999 KB must not show as "1024.0 KB"
    [InlineData(-5L, "0 B")]
    public void Format_ReturnsExplorerStyleText(long bytes, string expected)
    {
        Assert.Equal(expected, SizeFormatter.Format(bytes, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Format_LongMaxValue_UsesLargestUnit()
    {
        Assert.Equal("8.0 EB", SizeFormatter.Format(long.MaxValue, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Format_UsesDecimalSeparatorOfCulture()
    {
        Assert.Equal("1,5 KB", SizeFormatter.Format(1536, new CultureInfo("de-DE")));
    }
}

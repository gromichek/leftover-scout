using LeftoverScout.Core.Settings;
using LeftoverScout.Core.UI;

namespace LeftoverScout.Core.Tests.UI;

public class WindowBoundsValidatorTests
{
    private static readonly ScreenArea[] SingleScreen = [new ScreenArea(0, 0, 1920, 1080)];

    [Fact]
    public void IsVisible_WindowInsideScreen_ReturnsTrue()
    {
        Assert.True(WindowBoundsValidator.IsVisible(new WindowPlacement(100, 100, 900, 600, false), SingleScreen));
    }

    [Fact]
    public void IsVisible_WindowFarOffScreen_ReturnsFalse()
    {
        Assert.False(WindowBoundsValidator.IsVisible(new WindowPlacement(5000, 100, 900, 600, false), SingleScreen));
    }

    [Fact]
    public void IsVisible_OnlyAThinEdgeOverlaps_ReturnsFalse()
    {
        Assert.False(WindowBoundsValidator.IsVisible(new WindowPlacement(1900, 100, 900, 600, false), SingleScreen));
    }

    [Fact]
    public void IsVisible_WindowOnSecondScreen_ReturnsTrue()
    {
        ScreenArea[] screens = [new ScreenArea(0, 0, 1920, 1080), new ScreenArea(1920, 0, 1920, 1080)];

        Assert.True(WindowBoundsValidator.IsVisible(new WindowPlacement(2000, 50, 900, 600, false), screens));
    }

    [Fact]
    public void IsVisible_SecondScreenDisconnected_ReturnsFalse()
    {
        Assert.False(WindowBoundsValidator.IsVisible(new WindowPlacement(2000, 50, 900, 600, false), SingleScreen));
    }

    [Theory]
    [InlineData(double.NaN, 0, 900, 600)]
    [InlineData(0, 0, 0, 600)]
    [InlineData(0, 0, 900, -5)]
    [InlineData(0, double.PositiveInfinity, 900, 600)]
    public void IsVisible_InvalidNumbers_ReturnsFalse(double left, double top, double width, double height)
    {
        Assert.False(WindowBoundsValidator.IsVisible(new WindowPlacement(left, top, width, height, false), SingleScreen));
    }

    [Fact]
    public void IsVisible_WindowInGapBetweenMonitorsOfDifferentHeight_ReturnsFalse()
    {
        // A tall monitor on the left and a shorter one on the right. The virtual screen around both is
        // 3200 x 1080, but the area below the right monitor does not exist.
        ScreenArea[] screens = [new ScreenArea(0, 0, 1920, 1080), new ScreenArea(1920, 0, 1280, 720)];

        Assert.False(WindowBoundsValidator.IsVisible(new WindowPlacement(2000, 800, 900, 200, false), screens));
    }

    [Fact]
    public void IsVisible_WindowStraddlingTwoMonitorsButReachableOnOne_ReturnsTrue()
    {
        ScreenArea[] screens = [new ScreenArea(0, 0, 1920, 1080), new ScreenArea(1920, 0, 1280, 720)];

        Assert.True(WindowBoundsValidator.IsVisible(new WindowPlacement(1500, 100, 900, 600, false), screens));
    }

    [Fact]
    public void IsVisible_NoScreens_ReturnsFalse()
    {
        Assert.False(WindowBoundsValidator.IsVisible(new WindowPlacement(0, 0, 900, 600, false), []));
    }
}

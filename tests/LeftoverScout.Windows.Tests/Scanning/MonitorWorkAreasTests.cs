using LeftoverScout.Windows.Scanning;

namespace LeftoverScout.Windows.Tests.Scanning;

public class MonitorWorkAreasTests
{
    [Fact]
    public void GetPixelAreas_ReturnsAreasWithPositiveSize()
    {
        // A machine without a display may report no monitors; the call must still succeed.
        Assert.All(MonitorWorkAreas.GetPixelAreas(), area =>
        {
            Assert.True(area.Width > 0);
            Assert.True(area.Height > 0);
        });
    }
}

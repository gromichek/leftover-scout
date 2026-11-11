using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Tests.Cleanup;

public class RecycleBinPolicyTests
{
    [Fact]
    public void Evaluate_BinTurnedOff_ReturnsTurnedOff()
    {
        Assert.Equal(RecycleBinCheck.TurnedOff, RecycleBinPolicy.Evaluate(new RecycleBinSettings(true, 1000), 10));
    }

    [Fact]
    public void Evaluate_ItemLargerThanMaximum_ReturnsTooLarge()
    {
        Assert.Equal(RecycleBinCheck.TooLarge, RecycleBinPolicy.Evaluate(new RecycleBinSettings(false, 1000), 1001));
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1)]
    [InlineData(0)]
    public void Evaluate_ItemFits_ReturnsAllowed(long size)
    {
        Assert.Equal(RecycleBinCheck.Allowed, RecycleBinPolicy.Evaluate(new RecycleBinSettings(false, 1000), size));
    }

    [Fact]
    public void Evaluate_UnknownSettings_ReturnsAllowed()
    {
        Assert.Equal(RecycleBinCheck.Allowed, RecycleBinPolicy.Evaluate(RecycleBinSettings.Unknown, long.MaxValue));
    }

    [Fact]
    public void Evaluate_OffWinsOverSize()
    {
        Assert.Equal(RecycleBinCheck.TurnedOff, RecycleBinPolicy.Evaluate(new RecycleBinSettings(true, 10), 1000));
    }
}

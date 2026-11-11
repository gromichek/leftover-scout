using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Tests.Cleanup;

public class ShellOperationRulesTests
{
    [Theory]
    [InlineData(0u, true)]
    [InlineData(0x1u, true)]
    [InlineData(0x80u, false)]
    [InlineData(0x81u, false)]
    [InlineData(0x7Fu, true)]
    public void ShouldVetoDelete_RecycleBinMode_DependsOnTheRecycleFlag(uint flags, bool expected)
    {
        Assert.Equal(expected, ShellOperationRules.ShouldVetoDelete(CleanupMode.RecycleBin, flags));
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x80u)]
    public void ShouldVetoDelete_PermanentMode_NeverVetoes(uint flags)
    {
        Assert.False(ShellOperationRules.ShouldVetoDelete(CleanupMode.Permanent, flags));
    }

    [Theory]
    [InlineData(CleanupMode.RecycleBin, true, false, true)]
    [InlineData(CleanupMode.RecycleBin, true, true, false)]
    [InlineData(CleanupMode.RecycleBin, false, false, false)]
    [InlineData(CleanupMode.Permanent, true, false, false)]
    [InlineData(CleanupMode.Permanent, true, true, false)]
    public void IsUnexpectedPermanentDelete_OnlyWhenRecycledItemHasNoBinItem(CleanupMode mode, bool succeeded, bool hasNewItem, bool expected)
    {
        Assert.Equal(expected, ShellOperationRules.IsUnexpectedPermanentDelete(mode, succeeded, hasNewItem));
    }
}

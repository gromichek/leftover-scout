using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Cleanup;

public class CleanupStateTests
{
    private static ScanResult Result() => new(ScanCompletion.Completed, null, [], [], 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsRunning_Changes_RaisesChangedOnce()
    {
        var state = new CleanupState();
        int raised = 0;
        state.Changed += (_, _) => raised++;

        state.IsRunning = true;
        state.IsRunning = true;

        Assert.True(state.IsRunning);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void MarkStale_MarksOnlyThatResult_NewResultIsFresh()
    {
        var state = new CleanupState();
        ScanResult old = Result();
        ScanResult fresh = Result();
        int raised = 0;
        state.Changed += (_, _) => raised++;

        state.MarkStale(old);

        Assert.True(state.IsStale(old));
        Assert.False(state.IsStale(fresh));
        Assert.False(state.IsStale(null));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void MarkStale_Null_IsIgnored()
    {
        var state = new CleanupState();
        int raised = 0;
        state.Changed += (_, _) => raised++;

        state.MarkStale(null);

        Assert.Equal(0, raised);
    }
}

using LeftoverScout.Windows.SingleInstance;

namespace LeftoverScout.Windows.Tests.SingleInstance;

public class SingleInstanceGuardTests
{
    private static (string Mutex, string Event) UniqueNames()
    {
        string id = Guid.NewGuid().ToString("N");
        return ($@"Local\LeftoverScoutTest.{id}.Mutex", $@"Local\LeftoverScoutTest.{id}.Event");
    }

    [Fact]
    public void TryAcquire_SecondGuardWithSameName_ReturnsFalse()
    {
        (string mutex, string evt) = UniqueNames();
        using var first = new SingleInstanceGuard(mutex, evt);
        using var second = new SingleInstanceGuard(mutex, evt);

        Assert.True(first.TryAcquire());
        Assert.False(second.TryAcquire());
    }

    [Fact]
    public void TryAcquire_AfterFirstIsDisposed_Succeeds()
    {
        (string mutex, string evt) = UniqueNames();
        var first = new SingleInstanceGuard(mutex, evt);
        Assert.True(first.TryAcquire());
        first.Dispose();

        using var second = new SingleInstanceGuard(mutex, evt);

        Assert.True(second.TryAcquire());
    }

    [Fact]
    public void SignalFirstInstance_FirstInstanceListening_RunsCallback()
    {
        (string mutex, string evt) = UniqueNames();
        using var first = new SingleInstanceGuard(mutex, evt);
        using var second = new SingleInstanceGuard(mutex, evt);
        using var signaled = new ManualResetEventSlim();

        Assert.True(first.TryAcquire());
        first.StartListening(signaled.Set);
        Assert.False(second.TryAcquire());

        Assert.True(second.SignalFirstInstance());
        Assert.True(signaled.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SignalFirstInstance_NoFirstInstance_ReturnsFalse()
    {
        (string mutex, string evt) = UniqueNames();
        using var guard = new SingleInstanceGuard(mutex, evt);

        Assert.False(guard.SignalFirstInstance());
    }
}

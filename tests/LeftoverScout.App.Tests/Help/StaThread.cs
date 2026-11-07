using System.Runtime.ExceptionServices;

namespace LeftoverScout.App.Tests.Help;

/// <summary>Runs WPF code on a dedicated STA thread and rethrows its exception on the calling thread.</summary>
internal static class StaThread
{
    public static T Run<T>(Func<T> action)
    {
        T result = default!;
        ExceptionDispatchInfo? error = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                error = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        error?.Throw();
        return result;
    }

    public static void Run(Action action) => Run(() =>
    {
        action();
        return true;
    });
}

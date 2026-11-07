using System.Runtime.InteropServices;

namespace LeftoverScout.Windows.SingleInstance;

/// <summary>
/// Allows only one running instance per user session (decision D-019). The first instance owns a named
/// mutex and listens on a named event; a later instance signals that event and exits.
/// Both objects live in the <c>Local\</c> namespace, so they are per session.
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    /// <summary>Default mutex name (per user session).</summary>
    public const string DefaultMutexName = @"Local\LeftoverScout.SingleInstance";

    /// <summary>Default activation event name (per user session).</summary>
    public const string DefaultActivationEventName = @"Local\LeftoverScout.Activate";

    private const uint AllowSetForegroundForAnyProcess = 0xFFFFFFFF;

    private readonly string _mutexName;
    private readonly string _activationEventName;
    private Mutex? _mutex;
    private EventWaitHandle? _activationEvent;
    private RegisteredWaitHandle? _registeredWait;

    /// <summary>Creates a guard with the default names.</summary>
    public SingleInstanceGuard()
        : this(DefaultMutexName, DefaultActivationEventName)
    {
    }

    /// <summary>Creates a guard with custom names (used by tests so they do not collide with a running app).</summary>
    public SingleInstanceGuard(string mutexName, string activationEventName)
    {
        _mutexName = mutexName;
        _activationEventName = activationEventName;
    }

    /// <summary>
    /// Tries to become the first instance.
    /// </summary>
    /// <returns>True if this is the first instance; false if another instance is already running.</returns>
    public bool TryAcquire()
    {
        // Not initially owned: the "first instance" status is simply who created the mutex.
        _mutex = new Mutex(initiallyOwned: false, _mutexName, out bool createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        _activationEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, _activationEventName);
        return true;
    }

    /// <summary>
    /// Called by the first instance: runs <paramref name="onActivationRequested"/> on a thread-pool thread
    /// whenever another instance asks it to come to the front.
    /// </summary>
    public void StartListening(Action onActivationRequested)
    {
        if (_activationEvent is null)
        {
            throw new InvalidOperationException("Only the first instance can listen. Call TryAcquire first.");
        }

        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _activationEvent,
            (_, _) => onActivationRequested(),
            state: null,
            millisecondsTimeOutInterval: Timeout.Infinite,
            executeOnlyOnce: false);
    }

    /// <summary>
    /// Called by a second instance: asks the first instance to activate its window.
    /// </summary>
    /// <returns>True if the signal was delivered; false if the first instance could not be found.</returns>
    public bool SignalFirstInstance()
    {
        if (!EventWaitHandle.TryOpenExisting(_activationEventName, out EventWaitHandle? handle))
        {
            return false;
        }

        using (handle)
        {
            // This process was just started by the user, so Windows lets it hand the foreground right
            // to the first instance. See SetForegroundWindow / AllowSetForegroundWindow remarks.
            _ = AllowSetForegroundWindow(AllowSetForegroundForAnyProcess);
            return handle.Set();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _registeredWait?.Unregister(null);
        _registeredWait = null;
        _activationEvent?.Dispose();
        _activationEvent = null;
        _mutex?.Dispose();
        _mutex = null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);
}

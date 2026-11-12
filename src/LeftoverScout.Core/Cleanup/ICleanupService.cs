namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Removes the folders the user selected. This is the only service that deletes user data (see ARCHITECTURE.md).
/// </summary>
public interface ICleanupService
{
    /// <summary>
    /// Runs a cleanup session. Every item is checked again by the guard right before it is touched. Cancellation is
    /// checked between items only: the current item always completes (D-059).
    /// </summary>
    /// <param name="request">The selected items.</param>
    /// <param name="mode">Recycle Bin or permanent.</param>
    /// <param name="progress">Receives progress before each item.</param>
    /// <param name="cancellationToken">Stops the session before the next item begins.</param>
    /// <param name="itemCompleted">
    /// Called synchronously on the worker thread right after each item has its result (also for items that were not
    /// started because of a cancellation). The cleanup journal uses it. An exception from the callback is ignored.
    /// </param>
    Task<CleanupSessionResult> RunAsync(
        CleanupRequest request,
        CleanupMode mode,
        IProgress<CleanupProgress>? progress,
        CancellationToken cancellationToken,
        Action<CleanupItemResult>? itemCompleted = null);
}

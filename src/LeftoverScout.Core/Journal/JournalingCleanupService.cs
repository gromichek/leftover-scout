using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Journal;

/// <summary>
/// Wraps the cleanup service and records every session in the journal (task T-012): <c>Begin</c> before the first
/// item, <c>Record</c> after each item, <c>Complete</c> at the end, also after a cancellation or an exception.
/// The journal never throws into the cleanup, so a journal problem cannot stop or fail it.
/// </summary>
public sealed class JournalingCleanupService : ICleanupService
{
    private readonly ICleanupService _inner;
    private readonly ICleanupJournal _journal;

    /// <summary>Creates the decorator.</summary>
    /// <param name="inner">The real cleanup service.</param>
    /// <param name="journal">The journal.</param>
    public JournalingCleanupService(ICleanupService inner, ICleanupJournal journal)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(journal);
        _inner = inner;
        _journal = journal;
    }

    /// <inheritdoc />
    public async Task<CleanupSessionResult> RunAsync(
        CleanupRequest request,
        CleanupMode mode,
        IProgress<CleanupProgress>? progress,
        CancellationToken cancellationToken,
        Action<CleanupItemResult>? itemCompleted = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        JournalHandle handle = _journal.Begin(request, mode);
        try
        {
            CleanupSessionResult session = await _inner
                .RunAsync(
                    request,
                    mode,
                    progress,
                    cancellationToken,
                    result =>
                    {
                        _journal.Record(handle, result);
                        itemCompleted?.Invoke(result);
                    })
                .ConfigureAwait(false);

            _journal.Complete(handle, session);
            return session with { JournalWarning = handle.Warning };
        }
        catch (OperationCanceledException)
        {
            _journal.Abort(handle, JournalState.Cancelled);
            throw;
        }
        catch (Exception)
        {
            _journal.Abort(handle, JournalState.Failed);
            throw;
        }
    }
}

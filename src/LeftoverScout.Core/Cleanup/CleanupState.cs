using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// What the pages need to know about cleanups, shared by the Scan, Results, Findings and Cleanup pages: whether a
/// cleanup is running (the Scan page is then disabled) and which scan result is out of date because a cleanup ran
/// after it (decision D-068). Not thread-safe: use it from the UI thread.
/// </summary>
public sealed class CleanupState
{
    private bool _isRunning;
    private ScanResult? _staleResult;

    /// <summary>Raised after <see cref="IsRunning"/> or the stale result changed.</summary>
    public event EventHandler? Changed;

    /// <summary>True while a cleanup session is running.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Marks <paramref name="result"/> as out of date. It stays stale; a newer scan result is a different object and is
    /// not stale. A null result is ignored.
    /// </summary>
    public void MarkStale(ScanResult? result)
    {
        if (result is not null && !ReferenceEquals(_staleResult, result))
        {
            _staleResult = result;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>True when <paramref name="result"/> is the result that a cleanup made out of date.</summary>
    public bool IsStale(ScanResult? result) => result is not null && ReferenceEquals(_staleResult, result);
}

namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Runs scans of the current profile and keeps the last completed result in memory.
/// </summary>
public interface IScanService
{
    /// <summary>The last completed scan; null until one completes. A cancelled scan does not replace it.</summary>
    ScanResult? LastResult { get; }

    /// <summary>
    /// Raised after every scan that ends (completed or cancelled), possibly on a background thread.
    /// Read <see cref="LastResult"/> to see whether a new result was stored.
    /// </summary>
    event EventHandler? LastResultChanged;

    /// <summary>Scans the profile. Throws <see cref="InvalidOperationException"/> if a scan is already running.</summary>
    /// <param name="progress">Receives throttled progress reports; may be null.</param>
    /// <param name="cancellationToken">Cancels the scan; the result then has <see cref="ScanCompletion.Cancelled"/>.</param>
    Task<ScanResult> RunAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}

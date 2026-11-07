using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Scans the configured profile, logs start and end (totals only, no paths) and remembers the last result.
/// </summary>
public sealed class ScanService : IScanService
{
    private readonly ScanTarget _target;
    private readonly ProfileScanner _scanner;
    private readonly IPersonalFolderProvider _personalFolders;
    private readonly ILogger<ScanService> _logger;
    private int _running;

    /// <summary>Creates the service.</summary>
    public ScanService(
        ScanTarget target,
        IDirectoryReader reader,
        IPersonalFolderProvider personalFolders,
        ILogger<ScanService> logger,
        TimeProvider timeProvider)
    {
        _target = target;
        _personalFolders = personalFolders;
        _logger = logger;
        _scanner = new ProfileScanner(reader, timeProvider);
    }

    /// <inheritdoc />
    public ScanResult? LastResult { get; private set; }

    /// <inheritdoc />
    public async Task<ScanResult> RunAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            throw new InvalidOperationException("A scan is already running.");
        }

        try
        {
            _logger.LogInformation("Scan started.");

            var options = new ScanOptions { PersonalFolderPaths = _personalFolders.GetPersonalFolderPaths() };
            ScanResult result = await _scanner.ScanAsync(_target, options, progress, cancellationToken).ConfigureAwait(false);

            if (result.Root is { } root)
            {
                LastResult = result;
                _logger.LogInformation(
                    "Scan completed in {DurationMs} ms: {Folders} folders, {Files} files, {LocalBytes} local bytes, {CloudBytes} cloud-only bytes, {Errors} errors.",
                    (long)result.Duration.TotalMilliseconds,
                    root.SubfolderCount,
                    root.FileCount,
                    root.LocalSize,
                    root.CloudOnlySize,
                    result.Errors.Count);
            }
            else
            {
                _logger.LogInformation("Scan cancelled after {DurationMs} ms.", (long)result.Duration.TotalMilliseconds);
            }

            return result;
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}

using LeftoverScout.Core.Evidence;
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
    private readonly EvidenceCollector _evidenceCollector;
    private readonly ILogger<ScanService> _logger;
    private readonly TimeProvider _timeProvider;
    private int _running;

    /// <summary>Creates the service.</summary>
    public ScanService(
        ScanTarget target,
        IDirectoryReader reader,
        IPersonalFolderProvider personalFolders,
        EvidenceCollector evidenceCollector,
        ILogger<ScanService> logger,
        TimeProvider timeProvider)
    {
        _target = target;
        _personalFolders = personalFolders;
        _evidenceCollector = evidenceCollector;
        _logger = logger;
        _timeProvider = timeProvider;
        _scanner = new ProfileScanner(reader, timeProvider);
    }

    /// <summary>Progress text shown while the installed programs are still being collected after the profile scan.</summary>
    public const string CollectingProgramsText = "Collecting installed programs…";

    /// <inheritdoc />
    public ScanResult? LastResult { get; private set; }

    /// <inheritdoc />
    public event EventHandler? LastResultChanged;

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

            // The installed programs are collected while the profile is scanned (decision D-038).
            Task<EvidenceSnapshot> evidenceTask = _evidenceCollector.CollectAsync(cancellationToken);
            ScanResult result;
            EvidenceSnapshot? evidence = null;
            try
            {
                result = await _scanner.ScanAsync(_target, options, progress, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await ObserveAsync(evidenceTask).ConfigureAwait(false);
                throw;
            }

            if (result.Root is { } finishedRoot)
            {
                if (!evidenceTask.IsCompleted)
                {
                    progress?.Report(new ScanProgress(CollectingProgramsText, finishedRoot.SubfolderCount, finishedRoot.FileCount, finishedRoot.LocalSize));
                }

                try
                {
                    evidence = await evidenceTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Cancelled at the very end: the scan does not count as completed.
                    result = new ScanResult(ScanCompletion.Cancelled, null, [], [], 0, 0, 0, result.Duration);
                }
            }
            else
            {
                await ObserveAsync(evidenceTask).ConfigureAwait(false);
            }

            if (result.Root is { } root)
            {
                result = result with { CompletedAt = _timeProvider.GetUtcNow(), Evidence = evidence };
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

            LastResultChanged?.Invoke(this, EventArgs.Empty);
            return result;
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    // Waits for the evidence task and ignores its outcome (used when its result is not needed).
    private static async Task ObserveAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Cancelled or failed collection does not matter when the scan has no completed result.
        }
    }
}

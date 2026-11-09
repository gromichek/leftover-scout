using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Rules;
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
    private readonly RuleMatcher _ruleMatcher;
    private readonly LeftoverClassifier _classifier;
    private readonly ILogger<ScanService> _logger;
    private readonly TimeProvider _timeProvider;
    private int _running;

    /// <summary>Creates the service.</summary>
    public ScanService(
        ScanTarget target,
        IDirectoryReader reader,
        IPersonalFolderProvider personalFolders,
        EvidenceCollector evidenceCollector,
        RuleMatcher ruleMatcher,
        LeftoverClassifier classifier,
        ILogger<ScanService> logger,
        TimeProvider timeProvider)
    {
        _target = target;
        _personalFolders = personalFolders;
        _evidenceCollector = evidenceCollector;
        _ruleMatcher = ruleMatcher;
        _classifier = classifier;
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
                RuleMatchMap? ruleMatches = MatchRules(root, cancellationToken);
                result = result with
                {
                    CompletedAt = _timeProvider.GetUtcNow(),
                    Evidence = evidence,
                    RuleMatches = ruleMatches,
                    Leftovers = ClassifyLeftovers(root, evidence, ruleMatches, cancellationToken),
                };
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

    // Matches the rules once per completed scan. A failure here must not lose the scan result.
    private RuleMatchMap? MatchRules(FolderNode root, CancellationToken cancellationToken)
    {
        try
        {
            RuleMatchMap matches = _ruleMatcher.Match(root, cancellationToken);
            _logger.LogInformation("Rules matched {Folders} folders.", matches.Count);
            return matches;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Rules could not be matched ({ExceptionType}).", exception.GetType().Name);
            return null;
        }
    }

    // Detects leftovers once per completed scan. A failure here must not lose the scan result.
    private LeftoverMap? ClassifyLeftovers(FolderNode root, EvidenceSnapshot? evidence, RuleMatchMap? ruleMatches, CancellationToken cancellationToken)
    {
        try
        {
            LeftoverMap leftovers = _classifier.Classify(root, evidence, ruleMatches, cancellationToken);
            _logger.LogInformation("Leftover detection evaluated {Folders} folders.", leftovers.Count);
            return leftovers;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception exception)
        {
            _logger.LogWarning("Leftovers could not be detected ({ExceptionType}).", exception.GetType().Name);
            return null;
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

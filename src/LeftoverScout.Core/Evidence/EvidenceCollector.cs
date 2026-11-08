using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Runs all <see cref="IInstalledProgramSource"/> instances (in parallel) and gathers their results. A failing
/// source never fails the collection (decision D-038). Logs counts and duration only, never paths or names.
/// </summary>
public sealed class EvidenceCollector
{
    private readonly IReadOnlyList<IInstalledProgramSource> _sources;
    private readonly ILogger<EvidenceCollector> _logger;
    private readonly TimeProvider _time;

    /// <summary>Creates the collector.</summary>
    public EvidenceCollector(IEnumerable<IInstalledProgramSource> sources, ILogger<EvidenceCollector> logger, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources.ToList();
        _logger = logger;
        _time = timeProvider;
    }

    /// <summary>
    /// Starts every source on a background thread and returns when all have finished.
    /// Throws <see cref="OperationCanceledException"/> when <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public async Task<EvidenceSnapshot> CollectAsync(CancellationToken cancellationToken)
    {
        long start = _time.GetTimestamp();
        cancellationToken.ThrowIfCancellationRequested();

        // Task.Run starts all sources right away, so the caller can do other work meanwhile.
        List<Task<SourceOutcome>> tasks = _sources
            .Select(source => Task.Run(() => RunSource(source, cancellationToken), CancellationToken.None))
            .ToList();
        SourceOutcome[] outcomes = await Task.WhenAll(tasks).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var programs = new List<InstalledProgram>();
        var counts = new Dictionary<ProgramSource, int>();
        var errors = new List<EvidenceError>();
        foreach (SourceOutcome outcome in outcomes)
        {
            counts[outcome.Source] = counts.GetValueOrDefault(outcome.Source) + outcome.Programs.Count;
            programs.AddRange(outcome.Programs);
            if (outcome.Error is not null)
            {
                errors.Add(outcome.Error);
            }
        }

        TimeSpan duration = _time.GetElapsedTime(start);
        _logger.LogInformation(
            "Installed programs collected in {DurationMs} ms: {Registry} registry, {Store} store, {ProgramFolders} program folders, {StartMenu} start menu entries, {Errors} failed sources.",
            (long)duration.TotalMilliseconds,
            counts.GetValueOrDefault(ProgramSource.Registry),
            counts.GetValueOrDefault(ProgramSource.Store),
            counts.GetValueOrDefault(ProgramSource.ProgramFolder),
            counts.GetValueOrDefault(ProgramSource.StartMenu),
            errors.Count);

        return new EvidenceSnapshot(programs, counts, errors, _time.GetUtcNow(), duration);
    }

    private SourceOutcome RunSource(IInstalledProgramSource source, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new SourceOutcome(source.Source, source.Collect(cancellationToken), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new SourceOutcome(source.Source, [], null);
        }
        catch (Exception ex)
        {
            // Only the exception type is logged: messages can contain paths.
            _logger.LogWarning("Evidence source {Source} failed ({ExceptionType}).", source.Source, ex.GetType().Name);
            return new SourceOutcome(source.Source, [], new EvidenceError(source.Source, ErrorMessage(source.Source)));
        }
    }

    private static string ErrorMessage(ProgramSource source) => source switch
    {
        ProgramSource.Registry => "Registry entries could not be read.",
        ProgramSource.Store => "Store apps could not be read.",
        ProgramSource.ProgramFolder => "Program folders could not be read.",
        ProgramSource.StartMenu => "Start menu could not be read.",
        _ => "Some installed programs could not be read.",
    };

    private sealed record SourceOutcome(ProgramSource Source, IReadOnlyList<InstalledProgram> Programs, EvidenceError? Error);
}

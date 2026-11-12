using LeftoverScout.Core.Cleanup;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Journal;

/// <summary>
/// Writes the cleanup journal (task T-012, D-066): the file exists before the first item, is rewritten atomically
/// after every item and is finalized at the end. Writing never throws into the cleanup; a failure is logged (the
/// exception type only, never a path) and reported through <see cref="JournalHandle.Warning"/>.
/// </summary>
public sealed class CleanupJournal : ICleanupJournal
{
    private const string WriteWarning = "The cleanup journal could not be written completely. The cleanup itself was not affected.";

    private readonly IJournalStorage _storage;
    private readonly ActiveJournalSessions _active;
    private readonly TimeProvider _time;
    private readonly string _appVersion;
    private readonly ILogger<CleanupJournal> _logger;

    /// <summary>Creates the journal writer.</summary>
    /// <param name="storage">Where the files go.</param>
    /// <param name="active">Tracks the sessions running in this process.</param>
    /// <param name="time">The clock.</param>
    /// <param name="appVersion">The app version written into every file.</param>
    /// <param name="logger">The logger; paths are never logged.</param>
    public CleanupJournal(
        IJournalStorage storage,
        ActiveJournalSessions active,
        TimeProvider time,
        string appVersion,
        ILogger<CleanupJournal> logger)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(active);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(appVersion);
        ArgumentNullException.ThrowIfNull(logger);
        _storage = storage;
        _active = active;
        _time = time;
        _appVersion = appVersion;
        _logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler? SessionRecorded;

    /// <inheritdoc />
    public JournalHandle Begin(CleanupRequest request, CleanupMode mode)
    {
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset started = Local(_time.GetUtcNow());
        string profile = request.Target.ProfileRootPath;
        var document = new JournalDocument
        {
            SchemaVersion = JournalDocument.CurrentSchemaVersion,
            Id = Guid.NewGuid(),
            AppVersion = _appVersion,
            StartedAt = started,
            State = JournalState.InProgress,
            Mode = mode,
            ProfileRoot = profile,
            Items = RequestReducer.Reduce(request.Items).Items
                .Select(item => new JournalItem
                {
                    FullPath = item.FullPath,
                    RelativePath = CleanupService.RelativePath(profile, item.FullPath),
                    Kind = item.Kind,
                    Confidence = item.Confidence,
                    SizeAtScan = item.SizeBytes,
                })
                .ToList(),
        };

        var handle = new JournalHandle(document, JournalFileName.Create(started, document.Id));
        _active.Add(document.Id);
        lock (handle.Gate)
        {
            Write(handle);
        }

        SessionRecorded?.Invoke(this, EventArgs.Empty);
        return handle;
    }

    /// <inheritdoc />
    public void Record(JournalHandle handle, CleanupItemResult result)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(result);

        lock (handle.Gate)
        {
            Apply(handle.Document, result);
            Write(handle);
        }
    }

    /// <inheritdoc />
    public void Complete(JournalHandle handle, CleanupSessionResult session)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(session);

        lock (handle.Gate)
        {
            // Anything the callback missed is filled in from the final result.
            foreach (CleanupItemResult result in session.Items)
            {
                Apply(handle.Document, result);
            }

            Finish(handle, session.WasCancelled ? JournalState.Cancelled : JournalState.Completed);
        }

        SessionRecorded?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Abort(JournalHandle handle, JournalState state)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (state is not (JournalState.Cancelled or JournalState.Failed))
        {
            throw new ArgumentException("A session can only be aborted as cancelled or failed.", nameof(state));
        }

        lock (handle.Gate)
        {
            foreach (JournalItem item in handle.Document.Items.Where(item => item.Outcome == JournalOutcome.Pending))
            {
                item.Outcome = JournalOutcome.NotStarted;
                item.Reason = state == JournalState.Cancelled
                    ? "The cleanup was cancelled before this folder."
                    : "The cleanup stopped because of an error before this folder.";
            }

            Finish(handle, state);
        }

        SessionRecorded?.Invoke(this, EventArgs.Empty);
    }

    private static void Apply(JournalDocument document, CleanupItemResult result)
    {
        JournalItem? item = document.Items.FirstOrDefault(candidate =>
            string.Equals(candidate.FullPath, result.Item.FullPath, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            return;
        }

        item.Outcome = Enum.Parse<JournalOutcome>(result.Outcome.ToString());
        item.Reason = result.Reason;
        item.ItemsLeft = result.ItemsLeft;
        item.BytesRemoved = result.BytesRemoved;
        item.DeletedPermanentlyUnexpectedly = result.DeletedPermanentlyUnexpectedly;
    }

    private void Finish(JournalHandle handle, JournalState state)
    {
        handle.Document.State = state;
        handle.Document.EndedAt = Local(_time.GetUtcNow());
        Write(handle);
        _active.Remove(handle.Id);
    }

    private DateTimeOffset Local(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, _time.LocalTimeZone);

    // Totals are recomputed on every write, so the file is always consistent. Never throws.
    private void Write(JournalHandle handle)
    {
        try
        {
            JournalDocument document = handle.Document;
            document.Totals = new JournalTotals
            {
                ItemCount = document.Items.Count,
                BytesRemoved = document.Items.Sum(item => item.BytesRemoved),
                Outcomes = document.Items
                    .GroupBy(item => item.Outcome)
                    .OrderBy(group => group.Key)
                    .ToDictionary(group => group.Key.ToString(), group => group.Count()),
            };

            _storage.WriteAtomic(handle.FileName, JournalJson.Serialize(document));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Only the exception type: messages can contain paths.
            _logger.LogWarning("The cleanup journal could not be written ({ExceptionType}).", exception.GetType().Name);
            handle.Warning = WriteWarning;
        }
    }
}

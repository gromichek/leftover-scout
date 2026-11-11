using LeftoverScout.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Runs a cleanup session (task T-010): for each item it asks the guard again, applies the Recycle Bin pre-check
/// (D-062 layer 1), removes the folder through <see cref="IFolderRemover"/> and reports exactly what happened.
/// The actual deletion is done only by the Windows implementation of <see cref="IEntryRemover"/>. Cancellation is
/// checked between items only.
/// </summary>
public sealed class CleanupService : ICleanupService
{
    private readonly ICleanupGuard _guard;
    private readonly IFolderRemover _remover;
    private readonly IRecycleBinSettingsReader _binSettings;
    private readonly IDirectoryReader _reader;
    private readonly TimeProvider _time;
    private readonly ILogger<CleanupService> _logger;

    /// <summary>Creates the service.</summary>
    /// <param name="guard">The safety gate; called again before every item.</param>
    /// <param name="remover">Removes a folder.</param>
    /// <param name="binSettings">Reads the Recycle Bin settings of a drive.</param>
    /// <param name="reader">Lists folders, to measure what is left after a partial removal.</param>
    /// <param name="time">The clock.</param>
    /// <param name="logger">The logger; only counts are logged, never paths.</param>
    public CleanupService(
        ICleanupGuard guard,
        IFolderRemover remover,
        IRecycleBinSettingsReader binSettings,
        IDirectoryReader reader,
        TimeProvider time,
        ILogger<CleanupService> logger)
    {
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(remover);
        ArgumentNullException.ThrowIfNull(binSettings);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(logger);
        _guard = guard;
        _remover = remover;
        _binSettings = binSettings;
        _reader = reader;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<CleanupSessionResult> RunAsync(
        CleanupRequest request,
        CleanupMode mode,
        IProgress<CleanupProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Runs on a thread-pool thread: the UI thread never blocks. The token is not passed to Task.Run on purpose,
        // so that a cancelled session still returns a complete result with "not started" items.
        return Task.Run(() => Run(request, mode, progress, cancellationToken), CancellationToken.None);
    }

    private CleanupSessionResult Run(CleanupRequest request, CleanupMode mode, IProgress<CleanupProgress>? progress, CancellationToken token)
    {
        DateTime started = _time.GetUtcNow().UtcDateTime;
        IReadOnlyList<CleanupItem> items = RequestReducer.Reduce(request.Items).Items;
        string profile = request.Target.ProfileRootPath;
        var results = new List<CleanupItemResult>(items.Count);
        long bytesDone = 0;

        for (int index = 0; index < items.Count; index++)
        {
            CleanupItem item = items[index];
            if (token.IsCancellationRequested)
            {
                results.Add(new CleanupItemResult(item, CleanupOutcome.NotStarted, Reason: "The cleanup was cancelled before this folder."));
                continue;
            }

            progress?.Report(new CleanupProgress(index, items.Count, RelativePath(profile, item.FullPath), bytesDone));

            CleanupItemResult result = ProcessItem(request, item, mode);
            results.Add(result);
            bytesDone += result.BytesRemoved;
        }

        var session = new CleanupSessionResult(started, _time.GetUtcNow().UtcDateTime, mode, results);
        LogSession(session);
        return session;
    }

    private CleanupItemResult ProcessItem(CleanupRequest request, CleanupItem item, CleanupMode mode)
    {
        try
        {
            // Fresh guard check right before the item (never trusts an earlier check).
            CleanupGuardReport report = _guard.Check(new CleanupRequest(request.Target, [item]));
            GuardItemResult? guarded = report.Items.Count == 1 ? report.Items[0] : null;
            if (guarded is null || !guarded.Result.IsAllowed)
            {
                IReadOnlyList<BlockReason> reasons = guarded?.Result.BlockReasons ?? [BlockReason.Of(BlockReasonCode.CouldNotBeChecked)];
                return new CleanupItemResult(
                    item,
                    CleanupOutcome.Blocked,
                    Reason: string.Join(" ", reasons.Select(reason => reason.Text)),
                    BlockReasons: reasons);
            }

            if (mode == CleanupMode.RecycleBin)
            {
                RecycleBinCheck check = RecycleBinPolicy.Evaluate(_binSettings.Read(item.FullPath), item.SizeBytes);
                if (check == RecycleBinCheck.TurnedOff)
                {
                    return new CleanupItemResult(item, CleanupOutcome.SkippedRecycleBinOff, Reason: "The Recycle Bin is turned off for this drive.");
                }

                if (check == RecycleBinCheck.TooLarge)
                {
                    return new CleanupItemResult(item, CleanupOutcome.SkippedTooLargeForRecycleBin, Reason: "The folder is too large for the Recycle Bin.");
                }
            }

            return Map(item, mode, _remover.Remove(item.FullPath, mode));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Only the exception type is logged and reported: messages can contain paths.
            _logger.LogWarning("Cleanup of one item failed with {ExceptionType}.", exception.GetType().Name);
            return new CleanupItemResult(item, CleanupOutcome.Failed, Reason: $"Unexpected error ({exception.GetType().Name}).");
        }
    }

    private CleanupItemResult Map(CleanupItem item, CleanupMode mode, FolderRemoveResult result)
    {
        bool unexpected = result.DeletedPermanentlyUnexpectedly;
        string? unexpectedText = unexpected && mode == CleanupMode.RecycleBin
            ? "Windows deleted this permanently instead of using the Recycle Bin."
            : null;

        switch (result.Status)
        {
            case FolderRemoveStatus.Removed:
                return new CleanupItemResult(item, CleanupOutcome.Removed, item.SizeBytes, Reason: unexpectedText, DeletedPermanentlyUnexpectedly: unexpected);
            case FolderRemoveStatus.PartlyRemoved:
            {
                long remaining = FolderSizer.Measure(_reader, item.FullPath);
                long removed = Math.Max(0, item.SizeBytes - remaining);
                return new CleanupItemResult(item, CleanupOutcome.PartlyRemoved, removed, result.ItemsLeft, result.Reason ?? unexpectedText, DeletedPermanentlyUnexpectedly: unexpected);
            }

            case FolderRemoveStatus.NotRecycled:
                return new CleanupItemResult(item, CleanupOutcome.NotRecycled, Reason: result.Reason ?? "Windows could not move the folder to the Recycle Bin.");
            case FolderRemoveStatus.NotFound:
                return new CleanupItemResult(item, CleanupOutcome.NotFound, Reason: result.Reason);
            default:
                return new CleanupItemResult(item, CleanupOutcome.Failed, ItemsLeft: result.ItemsLeft, Reason: result.Reason ?? "The folder could not be removed.", DeletedPermanentlyUnexpectedly: unexpected);
        }
    }

    private static string RelativePath(string profileRoot, string fullPath)
    {
        string root = profileRoot.TrimEnd('\\', '/');
        return fullPath.Length > root.Length + 1 && fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? fullPath[(root.Length + 1)..]
            : fullPath;
    }

    // Counts and bytes only; paths are never logged.
    private void LogSession(CleanupSessionResult session)
    {
        string counts = string.Join(
            ", ",
            session.Items.GroupBy(item => item.Outcome).OrderBy(group => group.Key).Select(group => $"{group.Key}={group.Count()}"));

        _logger.LogInformation(
            "Cleanup session ({Mode}) finished: {Items} items ({Counts}), {Bytes} bytes removed, unexpected permanent deletion: {Unexpected}.",
            session.Mode,
            session.Items.Count,
            counts,
            session.BytesRemoved,
            session.DeletedPermanentlyUnexpectedly);
    }
}

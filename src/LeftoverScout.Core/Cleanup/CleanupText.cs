using LeftoverScout.Core.Formatting;

namespace LeftoverScout.Core.Cleanup;

/// <summary>The English texts of the Cleanup page that are not plain labels. Pure, so they are unit tested.</summary>
public static class CleanupText
{
    /// <summary>"n item(s)" with a thousands separator.</summary>
    public static string Items(int count) => $"{count:N0} {(count == 1 ? "item" : "items")}";

    /// <summary>"n item(s) · size".</summary>
    public static string Totals(int count, long bytes) => $"{Items(count)} · {SizeFormatter.Format(bytes)}";

    /// <summary>The selection line of the Findings page: <c>Selected: 2 items · 1.4 MB</c>.</summary>
    public static string SelectionLine(int count, long bytes) => $"Selected: {Totals(count, bytes)}";

    /// <summary>The banner of the Results and Findings pages after a cleanup (decision D-068).</summary>
    public const string StaleResultMessage = "Results are out of date after a cleanup — scan again.";

    /// <summary>Line about items that go together with another selected item; empty when there are none.</summary>
    public static string MergedLine(int mergedCount) => mergedCount switch
    {
        0 => string.Empty,
        1 => "1 item is inside another selected item and is included with it.",
        _ => $"{mergedCount:N0} items are inside other selected items and are included with them.",
    };

    /// <summary>The first question of the confirmation (the only one in Recycle Bin mode).</summary>
    public static string FirstQuestion(CleanupMode mode, int count, long bytes, int lowConfidenceCount)
    {
        string question = mode == CleanupMode.Permanent
            ? $"Delete {Items(count)} ({SizeFormatter.Format(bytes)}) permanently?"
            : $"Move {Items(count)} ({SizeFormatter.Format(bytes)}) to the Recycle Bin?";
        return lowConfidenceCount > 0
            ? $"{question}\n\nIncluding {Items(lowConfidenceCount)} with Low confidence: the program is probably still used."
            : question;
    }

    /// <summary>The second question, asked for permanent deletion only.</summary>
    public static string SecondQuestion(int count, long bytes) =>
        $"This cannot be undone. Delete {Items(count)} ({SizeFormatter.Format(bytes)}) permanently?";

    /// <summary>The question when the window is closed during a cleanup.</summary>
    public const string ExitQuestion = "A cleanup is running. Stop after the current item and exit?";

    /// <summary>The label of an outcome in the summary table.</summary>
    public static string Outcome(CleanupOutcome outcome) => outcome switch
    {
        CleanupOutcome.Removed => "Removed",
        CleanupOutcome.PartlyRemoved => "Partly removed",
        CleanupOutcome.SkippedTooLargeForRecycleBin => "Skipped (too large)",
        CleanupOutcome.SkippedRecycleBinOff => "Skipped (bin off)",
        CleanupOutcome.NotRecycled => "Not recycled",
        CleanupOutcome.Blocked => "Blocked",
        CleanupOutcome.NotFound => "Not found",
        CleanupOutcome.Failed => "Failed",
        _ => "Not started",
    };

    /// <summary>The reason column of the summary table; empty when the item was removed completely.</summary>
    public static string Reason(CleanupItemResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var parts = new List<string>();
        if (result.Outcome == CleanupOutcome.Blocked && result.BlockReasons is { Count: > 0 })
        {
            parts.AddRange(result.BlockReasons.Select(reason => reason.Text));
        }
        else if (!string.IsNullOrWhiteSpace(result.Reason))
        {
            parts.Add(result.Reason);
        }

        if (result.Outcome == CleanupOutcome.PartlyRemoved && result.ItemsLeft > 0)
        {
            parts.Add($"{result.ItemsLeft:N0} {(result.ItemsLeft == 1 ? "entry" : "entries")} left (in use or links).");
        }

        if (result.DeletedPermanentlyUnexpectedly)
        {
            parts.Add("Deleted permanently, not moved to the Recycle Bin.");
        }

        return string.Join(" ", parts);
    }

    /// <summary>The plain-words counts of a finished session, one sentence per outcome that occurred.</summary>
    public static IReadOnlyList<string> SummaryLines(CleanupSessionResult session)
    {
        ArgumentNullException.ThrowIfNull(session);

        bool recycle = session.Mode == CleanupMode.RecycleBin;
        var lines = new List<string>();
        void Add(CleanupOutcome outcome, Func<int, string> text)
        {
            int count = session.Count(outcome);
            if (count > 0)
            {
                lines.Add(text(count));
            }
        }

        Add(CleanupOutcome.Removed, n => recycle
            ? $"{Items(n)} moved to the Recycle Bin."
            : $"{Items(n)} deleted permanently.");
        Add(CleanupOutcome.PartlyRemoved, n => $"{Items(n)} removed only partly, because some files are in use.");
        Add(CleanupOutcome.SkippedTooLargeForRecycleBin, n => $"{Items(n)} skipped: too large for the Recycle Bin.");
        Add(CleanupOutcome.SkippedRecycleBinOff, n => $"{Items(n)} skipped: the Recycle Bin is turned off for the drive.");
        Add(CleanupOutcome.NotRecycled, n => $"{Items(n)} not moved: Windows could not use the Recycle Bin.");
        Add(CleanupOutcome.Blocked, n => $"{Items(n)} blocked by the safety check and left untouched.");
        Add(CleanupOutcome.NotFound, n => $"{Items(n)} not found: already gone.");
        Add(CleanupOutcome.Failed, n => $"{Items(n)} could not be removed.");
        Add(CleanupOutcome.NotStarted, n => $"{Items(n)} not started, because the cleanup was cancelled.");
        return lines;
    }

    /// <summary>The note about the Recycle Bin shown for the Recycle Bin mode.</summary>
    public const string RecycleBinNote = "Space is freed when you empty the Recycle Bin.";

    /// <summary>The warning shown when a Recycle Bin cleanup deleted something permanently anyway.</summary>
    public const string UnexpectedPermanentWarning =
        "Windows deleted at least one item permanently instead of moving it to the Recycle Bin. Those items cannot be restored.";
}

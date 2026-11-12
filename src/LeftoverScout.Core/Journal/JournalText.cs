using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;

namespace LeftoverScout.Core.Journal;

/// <summary>The English texts the History page shows for journals. Pure, so they are unit tested.</summary>
public static class JournalText
{
    /// <summary>"Recycle Bin" or "Permanently".</summary>
    public static string Mode(CleanupMode mode) => mode == CleanupMode.Permanent ? "Permanently" : "Recycle Bin";

    /// <summary>The kind as shown in the History table.</summary>
    public static string Kind(FindingKind kind) => kind switch
    {
        FindingKind.Leftover => "Leftover",
        FindingKind.Cache => "Cache",
        FindingKind.Temp => "Temp",
        FindingKind.Logs => "Logs",
        _ => "Crash dumps",
    };

    /// <summary>The outcome as shown in the History table.</summary>
    public static string Outcome(JournalOutcome outcome) => outcome switch
    {
        JournalOutcome.Removed => "Removed",
        JournalOutcome.PartlyRemoved => "Partly removed",
        JournalOutcome.SkippedTooLargeForRecycleBin => "Skipped (too large)",
        JournalOutcome.SkippedRecycleBinOff => "Skipped (bin off)",
        JournalOutcome.NotRecycled => "Not recycled",
        JournalOutcome.Blocked => "Blocked",
        JournalOutcome.NotFound => "Not found",
        JournalOutcome.Failed => "Failed",
        JournalOutcome.NotStarted => "Not started",
        _ => "Pending",
    };

    /// <summary>
    /// The "Result" column of the sessions list, for example <c>12 removed · 1 partly · 2 skipped</c>, or
    /// <c>Interrupted</c>, <c>In progress</c>, <c>Unreadable journal</c>.
    /// </summary>
    public static string Result(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        switch (entry.Status)
        {
            case JournalEntryStatus.Unreadable:
                return "Unreadable journal";
            case JournalEntryStatus.Interrupted:
                return "Interrupted";
            case JournalEntryStatus.InProgress:
                return "In progress";
        }

        string counts = Counts(entry.Document!);
        string prefix = entry.Status switch
        {
            JournalEntryStatus.Cancelled => "Cancelled",
            JournalEntryStatus.Failed => "Failed",
            _ => string.Empty,
        };

        if (prefix.Length == 0)
        {
            return counts.Length > 0 ? counts : "Nothing to do";
        }

        return counts.Length > 0 ? $"{prefix} · {counts}" : prefix;
    }

    private static string Counts(JournalDocument document)
    {
        int Count(params JournalOutcome[] outcomes) => document.Items.Count(item => outcomes.Contains(item.Outcome));

        var parts = new List<string>();
        void Add(int count, string label)
        {
            if (count > 0)
            {
                parts.Add($"{count} {label}");
            }
        }

        Add(Count(JournalOutcome.Removed), "removed");
        Add(Count(JournalOutcome.PartlyRemoved), "partly");
        Add(
            Count(JournalOutcome.SkippedTooLargeForRecycleBin, JournalOutcome.SkippedRecycleBinOff, JournalOutcome.NotRecycled),
            "skipped");
        Add(Count(JournalOutcome.Blocked), "blocked");
        Add(Count(JournalOutcome.NotFound), "not found");
        Add(Count(JournalOutcome.Failed), "failed");
        Add(Count(JournalOutcome.NotStarted, JournalOutcome.Pending), "not started");
        return string.Join(" · ", parts);
    }
}

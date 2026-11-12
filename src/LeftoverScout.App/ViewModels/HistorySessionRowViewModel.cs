using System.Globalization;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Journal;

namespace LeftoverScout.App.ViewModels;

/// <summary>A row of the sessions list on the History page.</summary>
public sealed class HistorySessionRowViewModel
{
    /// <summary>Creates the row for a journal entry.</summary>
    public HistorySessionRowViewModel(JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;

        JournalDocument? document = entry.Document;
        SortTime = entry.SortTime;
        DateText = document is null ? "Unknown" : ResultsFormat.LocalDateTime(document.StartedAt);
        ModeText = document is null ? string.Empty : JournalText.Mode(document.Mode);
        ItemCount = document?.Items.Count ?? 0;
        ItemsText = document is null ? string.Empty : ItemCount.ToString("N0", CultureInfo.CurrentCulture);
        BytesRemoved = document?.Totals.BytesRemoved ?? 0;
        RemovedText = document is null ? string.Empty : SizeFormatter.Format(BytesRemoved);
        ResultText = JournalText.Result(entry);
    }

    /// <summary>The journal entry behind the row.</summary>
    public JournalEntry Entry { get; }

    /// <summary>The file name of the journal (no path).</summary>
    public string FileName => Entry.FileName;

    /// <summary>Start time (sort value).</summary>
    public DateTime SortTime { get; }

    /// <summary>Start time as text.</summary>
    public string DateText { get; }

    /// <summary>"Recycle Bin" or "Permanently"; empty for an unreadable journal.</summary>
    public string ModeText { get; }

    /// <summary>Number of items (sort value).</summary>
    public int ItemCount { get; }

    /// <summary>Number of items as text.</summary>
    public string ItemsText { get; }

    /// <summary>Bytes removed (sort value).</summary>
    public long BytesRemoved { get; }

    /// <summary>Size removed as text.</summary>
    public string RemovedText { get; }

    /// <summary>The result, for example "12 removed · 1 partly · 2 skipped".</summary>
    public string ResultText { get; }
}

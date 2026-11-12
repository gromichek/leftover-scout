using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Journal;

namespace LeftoverScout.App.ViewModels;

/// <summary>A row of the details table on the History page.</summary>
public sealed class HistoryItemRowViewModel
{
    /// <summary>Creates the row for a journal item.</summary>
    public HistoryItemRowViewModel(JournalItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Item = item;
        KindText = JournalText.Kind(item.Kind);
        ConfidenceText = item.Confidence?.ToString() ?? string.Empty;
        SizeText = SizeFormatter.Format(item.SizeAtScan);
        OutcomeText = JournalText.Outcome(item.Outcome);
        ReasonText = item.Reason ?? string.Empty;
    }

    /// <summary>The journal item behind the row.</summary>
    public JournalItem Item { get; }

    /// <summary>Full path (copied by "Copy path").</summary>
    public string FullPath => Item.FullPath;

    /// <summary>Path relative to the profile root.</summary>
    public string RelativePath => Item.RelativePath;

    /// <summary>The kind, for example "Cache".</summary>
    public string KindText { get; }

    /// <summary>The confidence; empty for cache kinds.</summary>
    public string ConfidenceText { get; }

    /// <summary>Size at scan (sort value).</summary>
    public long SizeBytes => Item.SizeAtScan;

    /// <summary>Size at scan as text.</summary>
    public string SizeText { get; }

    /// <summary>The outcome as text.</summary>
    public string OutcomeText { get; }

    /// <summary>The reason; empty for a complete removal.</summary>
    public string ReasonText { get; }
}

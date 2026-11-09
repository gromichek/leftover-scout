using System.Globalization;
using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Formatting;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// A row of the Findings table. Numeric and date columns expose the raw value for sorting and a formatted text for display.
/// </summary>
public sealed class FindingRowViewModel
{
    /// <summary>Creates the row view model.</summary>
    public FindingRowViewModel(FindingEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
        SizeText = SizeFormatter.Format(Finding.LocalSize);
        FilesText = Finding.FileCount.ToString("N0", CultureInfo.CurrentCulture);
        LastModifiedText = Finding.NewestWriteTimeUtc is { } utc ? ResultsFormat.LocalDateTime(utc) : string.Empty;
        ExplanationText = string.Join("\n", Finding.Explanation);
        RuleText = Finding.RuleDescription is { } description ? $"{description} ({Finding.RuleId})" : string.Empty;
    }

    /// <summary>The underlying entry.</summary>
    public FindingEntry Entry { get; }

    /// <summary>The finding.</summary>
    public Finding Finding => Entry.Finding;

    /// <summary>Path below the profile folder.</summary>
    public string Path => Finding.RelativePath;

    /// <summary>Full path (used by the actions and the details panel).</summary>
    public string FullPath => Finding.FullPath;

    /// <summary>Leftover, Cache, Temp, Logs or Crash dumps.</summary>
    public string KindText => Finding.KindText;

    /// <summary>High, Medium or Low; empty for cache kinds.</summary>
    public string ConfidenceText => Finding.ConfidenceText;

    /// <summary>Sort value of the confidence: cache kinds 0, then Low, Medium, High.</summary>
    public int ConfidenceRank => Finding.Confidence switch
    {
        LeftoverConfidence.Low => 1,
        LeftoverConfidence.Medium => 2,
        LeftoverConfidence.High => 3,
        _ => 0,
    };

    /// <summary>Local size in bytes (sort value).</summary>
    public long Size => Finding.LocalSize;

    /// <summary>Size in Explorer format.</summary>
    public string SizeText { get; }

    /// <summary>Number of files, formatted.</summary>
    public string FilesText { get; }

    /// <summary>Newest write time in UTC (sort value); null when unknown.</summary>
    public DateTime? LastModified => Finding.NewestWriteTimeUtc;

    /// <summary>Newest write time in local time.</summary>
    public string LastModifiedText { get; }

    /// <summary>The program; empty when unknown.</summary>
    public string Program => Finding.ProgramName ?? string.Empty;

    /// <summary>The explanation lines, one per line.</summary>
    public string ExplanationText { get; }

    /// <summary>The matched rule (description and id); empty when no rule matched.</summary>
    public string RuleText { get; }

    /// <summary>True when a rule matched.</summary>
    public bool HasRule => RuleText.Length > 0;

    /// <summary>The "Inside another finding" note; empty when the item is not nested in a visible finding.</summary>
    public string InsideNote => Entry.InsideNote;

    /// <summary>True when <see cref="InsideNote"/> has text.</summary>
    public bool IsNested => Entry.InsideOf is not null;
}

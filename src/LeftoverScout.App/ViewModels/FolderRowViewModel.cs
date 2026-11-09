using System.Globalization;
using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Results;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// A row of the folder table. Numeric and date columns expose the raw value for sorting
/// and a formatted text for display.
/// </summary>
public sealed class FolderRowViewModel
{
    /// <summary>Creates the row view model.</summary>
    public FolderRowViewModel(FolderRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        Row = row;
        SizeText = SizeFormatter.Format(row.LocalSize);
        PercentText = string.Create(CultureInfo.CurrentCulture, $"{row.PercentOfParent:0.0} %");
        FilesText = row.FileCount.ToString("N0", CultureInfo.CurrentCulture);
        LastModifiedText = row.NewestWriteTimeUtc is { } utc ? ResultsFormat.LocalDateTime(utc) : string.Empty;
        NotesText = FolderTagText.Describe(row.Tags, row.RuleMatch, row.Leftover);
        NotesTooltip = FolderTagText.Explain(row.Tags, row.RuleMatch, row.Leftover);
    }

    /// <summary>The underlying row.</summary>
    public FolderRow Row { get; }

    /// <summary>Folder name.</summary>
    public string Name => Row.Name;

    /// <summary>Full path (the current folder for the files row).</summary>
    public string FullPath => Row.FullPath;

    /// <summary>Local size in bytes (sort value).</summary>
    public long Size => Row.LocalSize;

    /// <summary>Size in Explorer format.</summary>
    public string SizeText { get; }

    /// <summary>Share of the parent in percent (sort value and bar value).</summary>
    public double PercentOfParent => Row.PercentOfParent;

    /// <summary>Share of the parent, for example "12.3 %".</summary>
    public string PercentText { get; }

    /// <summary>Number of files (sort value).</summary>
    public long Files => Row.FileCount;

    /// <summary>Number of files, formatted.</summary>
    public string FilesText { get; }

    /// <summary>Newest write time in UTC (sort value); null when unknown.</summary>
    public DateTime? LastModified => Row.NewestWriteTimeUtc;

    /// <summary>Newest write time in local time, short date and time.</summary>
    public string LastModifiedText { get; }

    /// <summary>Notes tags and the rule label (decision D-043), joined with ", ".</summary>
    public string NotesText { get; }

    /// <summary>One sentence per tag, then the matched rule descriptions; empty when there is nothing to show.</summary>
    public string NotesTooltip { get; }

    /// <summary>True when double-click or Enter opens the row.</summary>
    public bool CanOpen => Row.CanOpen;
}

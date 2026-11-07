using LeftoverScout.Core.Formatting;
using LeftoverScout.Core.Results;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// A row of the Large files table.
/// </summary>
public sealed class LargeFileRowViewModel
{
    /// <summary>Creates the row view model.</summary>
    public LargeFileRowViewModel(LargeFileRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        Row = row;
        SizeText = SizeFormatter.Format(row.Size);
        LastModifiedText = ResultsFormat.LocalDateTime(row.LastWriteTimeUtc);
    }

    /// <summary>The underlying row.</summary>
    public LargeFileRow Row { get; }

    /// <summary>Path relative to the profile root.</summary>
    public string RelativePath => Row.RelativePath;

    /// <summary>Full path, shown as tooltip and used by the actions.</summary>
    public string FullPath => Row.FullPath;

    /// <summary>Size in bytes (sort value).</summary>
    public long Size => Row.Size;

    /// <summary>Size in Explorer format.</summary>
    public string SizeText { get; }

    /// <summary>Last write time in UTC (sort value).</summary>
    public DateTime LastModified => Row.LastWriteTimeUtc;

    /// <summary>Last write time in local time, short date and time.</summary>
    public string LastModifiedText { get; }
}

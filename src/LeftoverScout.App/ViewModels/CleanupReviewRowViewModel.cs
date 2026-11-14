using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Formatting;

namespace LeftoverScout.App.ViewModels;

/// <summary>A row of the review table on the Cleanup page.</summary>
public sealed class CleanupReviewRowViewModel
{
    /// <summary>Creates the row.</summary>
    public CleanupReviewRowViewModel(CleanupReviewRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        Row = row;
        SizeText = SizeFormatter.Format(row.Item.SizeBytes);
    }

    /// <summary>The underlying row.</summary>
    public CleanupReviewRow Row { get; }

    /// <summary>Path below the profile folder.</summary>
    public string Path => Row.RelativePath;

    /// <summary>Leftover, Cache, Temp, Logs or Crash dumps.</summary>
    public string KindText => Row.KindText;

    /// <summary>High, Medium or Low; empty for cache kinds.</summary>
    public string ConfidenceText => Row.ConfidenceText;

    /// <summary>Size in bytes (sort value).</summary>
    public long Size => Row.Item.SizeBytes;

    /// <summary>Size in Explorer format.</summary>
    public string SizeText { get; }

    /// <summary>Ready, Ready with warnings, or Blocked with the reason.</summary>
    public string StatusText => Row.StatusText;

    /// <summary>True when the item will be cleaned.</summary>
    public bool IsReady => Row.IsReady;

    /// <summary>True when the guard blocked the item.</summary>
    public bool IsBlocked => !Row.IsReady;
}

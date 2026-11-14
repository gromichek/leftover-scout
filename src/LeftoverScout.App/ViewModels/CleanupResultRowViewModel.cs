using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Formatting;

namespace LeftoverScout.App.ViewModels;

/// <summary>A row of the result table in the summary of the Cleanup page.</summary>
public sealed class CleanupResultRowViewModel
{
    /// <summary>Creates the row.</summary>
    /// <param name="result">The result of one item.</param>
    /// <param name="relativePath">The path below the profile folder.</param>
    public CleanupResultRowViewModel(CleanupItemResult result, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(relativePath);
        Path = relativePath;
        SizeText = SizeFormatter.Format(result.Item.SizeBytes);
        OutcomeText = CleanupText.Outcome(result.Outcome);
        ReasonText = CleanupText.Reason(result);
    }

    /// <summary>Path below the profile folder.</summary>
    public string Path { get; }

    /// <summary>Size at the time of the scan.</summary>
    public string SizeText { get; }

    /// <summary>What happened, in words.</summary>
    public string OutcomeText { get; }

    /// <summary>Why, when the item was not removed completely; otherwise empty.</summary>
    public string ReasonText { get; }
}

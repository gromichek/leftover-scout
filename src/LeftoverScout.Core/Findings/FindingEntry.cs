namespace LeftoverScout.Core.Findings;

/// <summary>A visible finding and, when it lies inside another visible finding, that outer finding.</summary>
/// <param name="Finding">The finding.</param>
/// <param name="InsideOf">The nearest visible finding that contains this one; null when none.</param>
public sealed record FindingEntry(Finding Finding, Finding? InsideOf)
{
    /// <summary>The note for the details panel, or empty: <c>Inside another finding: &lt;path&gt;</c>.</summary>
    public string InsideNote => InsideOf is null ? string.Empty : $"Inside another finding: {InsideOf.RelativePath}";
}

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// A label with a value, shown as one line on the Scan page.
/// </summary>
/// <param name="Label">What the value means.</param>
/// <param name="Value">The formatted value.</param>
public sealed record SummaryItem(string Label, string Value);

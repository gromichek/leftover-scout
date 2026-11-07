namespace LeftoverScout.App.ViewModels;

/// <summary>
/// A page that is not implemented yet. It tells the user in which phase it arrives and what it will do.
/// </summary>
public sealed class PlaceholderPageViewModel : PageViewModel
{
    /// <summary>Creates a placeholder page.</summary>
    /// <param name="title">Navigation title.</param>
    /// <param name="phase">Phase number in which the page becomes available.</param>
    /// <param name="description">One sentence about what the page will do.</param>
    public PlaceholderPageViewModel(string title, int phase, string description)
        : base(title)
    {
        Availability = $"Available in Phase {phase}";
        Description = description;
    }

    /// <summary>Short availability text, for example "Available in Phase 1".</summary>
    public string Availability { get; }

    /// <summary>One sentence about what the page will do.</summary>
    public string Description { get; }
}

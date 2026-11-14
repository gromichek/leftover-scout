namespace LeftoverScout.App.ViewModels;

/// <summary>The states of the Cleanup page (decision D-059).</summary>
public enum CleanupPageState
{
    /// <summary>Nothing is selected: the page points to Findings.</summary>
    NothingSelected,

    /// <summary>The safety check and the running-program check are running.</summary>
    Checking,

    /// <summary>The user reviews the items, chooses the mode and starts.</summary>
    Review,

    /// <summary>A cleanup is running.</summary>
    Running,

    /// <summary>A cleanup has ended and its summary is shown.</summary>
    Summary,
}

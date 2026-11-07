using CommunityToolkit.Mvvm.ComponentModel;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// Base class of every page shown in the content area of the main window.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    /// <summary>Creates a page with the given navigation title.</summary>
    protected PageViewModel(string title)
    {
        Title = title;
    }

    /// <summary>Text shown in the navigation list and as the page heading.</summary>
    public string Title { get; }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// Base class of every page shown in the content area of the main window.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    /// <summary>Creates a page with the given navigation title and help topic.</summary>
    /// <param name="title">Navigation title.</param>
    /// <param name="helpTopicId">Id of the help topic of this page, see <c>HelpTopics</c> (decision D-030).</param>
    protected PageViewModel(string title, string helpTopicId)
    {
        Title = title;
        HelpTopicId = helpTopicId;
    }

    /// <summary>Id of the help topic that the ? button and F1 open for this page.</summary>
    public string HelpTopicId { get; }

    /// <summary>Text shown in the navigation list and as the page heading.</summary>
    public string Title { get; }
}

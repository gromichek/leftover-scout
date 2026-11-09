using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Help;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// View model of the main window: left navigation, current page, status bar and info bar.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInfoBarVisible))]
    private string _infoBarText = string.Empty;

    /// <summary>Creates the main view model.</summary>
    /// <param name="pages">All pages in navigation order; the first one is shown initially.</param>
    /// <param name="dataLocation">Used to explain when the fallback data folder is in use.</param>
    public MainViewModel(IEnumerable<PageViewModel> pages, AppDataLocation dataLocation)
    {
        Pages = pages.ToList();
        if (Pages.Count == 0)
        {
            throw new ArgumentException("At least one page is required.", nameof(pages));
        }

        _currentPage = Pages[0];

        foreach (ScanPageViewModel scanPage in Pages.OfType<ScanPageViewModel>())
        {
            scanPage.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ScanPageViewModel.StatusMessage) && !string.IsNullOrEmpty(scanPage.StatusMessage))
                {
                    StatusText = scanPage.StatusMessage;
                }
            };
        }

        foreach (ResultsPageViewModel resultsPage in Pages.OfType<ResultsPageViewModel>())
        {
            resultsPage.GoToScanRequested += (_, _) =>
            {
                ScanPageViewModel? scanPage = Pages.OfType<ScanPageViewModel>().FirstOrDefault();
                if (scanPage is not null)
                {
                    CurrentPage = scanPage;
                }
            };

            resultsPage.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ResultsPageViewModel.StatusMessage) && !string.IsNullOrEmpty(resultsPage.StatusMessage))
                {
                    StatusText = resultsPage.StatusMessage;
                }
            };
        }

        foreach (ProgramsPageViewModel programsPage in Pages.OfType<ProgramsPageViewModel>())
        {
            programsPage.GoToScanRequested += (_, _) =>
            {
                ScanPageViewModel? scanPage = Pages.OfType<ScanPageViewModel>().FirstOrDefault();
                if (scanPage is not null)
                {
                    CurrentPage = scanPage;
                }
            };

            programsPage.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ProgramsPageViewModel.StatusMessage) && !string.IsNullOrEmpty(programsPage.StatusMessage))
                {
                    StatusText = programsPage.StatusMessage;
                }
            };
        }

        foreach (FindingsPageViewModel findingsPage in Pages.OfType<FindingsPageViewModel>())
        {
            findingsPage.GoToScanRequested += (_, _) =>
            {
                ScanPageViewModel? scanPage = Pages.OfType<ScanPageViewModel>().FirstOrDefault();
                if (scanPage is not null)
                {
                    CurrentPage = scanPage;
                }
            };

            findingsPage.ShowInResultsRequested += (_, node) =>
            {
                ResultsPageViewModel? resultsPage = Pages.OfType<ResultsPageViewModel>().FirstOrDefault();
                if (resultsPage is not null && resultsPage.ShowFolder(node))
                {
                    CurrentPage = resultsPage;
                }
            };

            findingsPage.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FindingsPageViewModel.StatusMessage) && !string.IsNullOrEmpty(findingsPage.StatusMessage))
                {
                    StatusText = findingsPage.StatusMessage;
                }
            };
        }

        if (dataLocation.IsFallback)
        {
            _infoBarText = $"Settings are stored in {dataLocation.Path} because the program folder is not writable.";
        }
    }

    /// <summary>Window title.</summary>
    public string Title => "LeftoverScout";

    /// <summary>All pages in navigation order.</summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>True when the info bar has a message to show.</summary>
    public bool IsInfoBarVisible => !string.IsNullOrEmpty(InfoBarText);

    [RelayCommand]
    private void Navigate(PageViewModel page) => CurrentPage = page;

    /// <summary>
    /// Opens the Help page at the topic of the current page (the ? button and F1, decision D-030).
    /// On the Help page itself it opens the topic about Help.
    /// </summary>
    [RelayCommand]
    private void OpenHelp()
    {
        HelpPageViewModel? help = Pages.OfType<HelpPageViewModel>().FirstOrDefault();
        if (help is null)
        {
            return;
        }

        help.ShowTopic(CurrentPage is HelpPageViewModel ? HelpTopics.Help : CurrentPage.HelpTopicId);
        CurrentPage = help;
    }

    [RelayCommand]
    private void DismissInfoBar() => InfoBarText = string.Empty;
}

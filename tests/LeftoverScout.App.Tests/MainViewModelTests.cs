using LeftoverScout.App.ViewModels;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.Tests;

public class MainViewModelTests
{
    private static readonly AppDataLocation Portable = new(@"C:\Apps\SampleApp\data", IsFallback: false);

    private static PlaceholderPageViewModel[] CreatePages() =>
    [
        new("Scan", "scan", 1, "Scan description."),
        new("Results", "results", 1, "Results description."),
        new("Cleanup", "cleanup", 3, "Cleanup description."),
    ];

    [Fact]
    public void Constructor_SelectsFirstPage()
    {
        PlaceholderPageViewModel[] pages = CreatePages();

        var viewModel = new MainViewModel(pages, Portable);

        Assert.Same(pages[0], viewModel.CurrentPage);
        Assert.Equal(3, viewModel.Pages.Count);
    }

    [Fact]
    public void NavigateCommand_SwitchesCurrentPage()
    {
        PlaceholderPageViewModel[] pages = CreatePages();
        var viewModel = new MainViewModel(pages, Portable);

        viewModel.NavigateCommand.Execute(pages[2]);

        Assert.Same(pages[2], viewModel.CurrentPage);
    }

    [Fact]
    public void CurrentPage_Changed_RaisesPropertyChanged()
    {
        PlaceholderPageViewModel[] pages = CreatePages();
        var viewModel = new MainViewModel(pages, Portable);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.CurrentPage = pages[1];

        Assert.Contains(nameof(MainViewModel.CurrentPage), changed);
    }

    [Fact]
    public void Constructor_NoPages_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MainViewModel([], Portable));
    }

    [Fact]
    public void InfoBar_PortableFolder_IsHidden()
    {
        var viewModel = new MainViewModel(CreatePages(), Portable);

        Assert.False(viewModel.IsInfoBarVisible);
    }

    [Fact]
    public void InfoBar_FallbackFolder_ShowsMessageWithPathAndCanBeDismissed()
    {
        var fallback = new AppDataLocation(@"C:\Users\SampleUser\AppData\Local\LeftoverScout", IsFallback: true);
        var viewModel = new MainViewModel(CreatePages(), fallback);

        Assert.True(viewModel.IsInfoBarVisible);
        Assert.Equal(
            @"Settings are stored in C:\Users\SampleUser\AppData\Local\LeftoverScout because the program folder is not writable.",
            viewModel.InfoBarText);

        viewModel.DismissInfoBarCommand.Execute(null);

        Assert.False(viewModel.IsInfoBarVisible);
    }
}

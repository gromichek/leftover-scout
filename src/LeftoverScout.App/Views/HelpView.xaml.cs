using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LeftoverScout.App.Help;
using LeftoverScout.App.ViewModels;

namespace LeftoverScout.App.Views;

/// <summary>
/// The Help page view. Pure view code: it renders the Markdown of the view model and forwards link clicks to it.
/// </summary>
public partial class HelpView : UserControl
{
    private HelpPageViewModel? _viewModel;

    /// <summary>Creates the view.</summary>
    public HelpView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as HelpPageViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ShowDocument();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HelpPageViewModel.Markdown))
        {
            ShowDocument();
        }
    }

    private void ShowDocument()
    {
        HelpPageViewModel? viewModel = _viewModel;
        if (viewModel is null)
        {
            Viewer.Document = null;
            return;
        }

        Viewer.Document = new HelpDocumentBuilder(viewModel.OpenLink).Build(viewModel.Markdown);
    }
}

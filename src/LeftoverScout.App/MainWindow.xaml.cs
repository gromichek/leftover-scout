using System.Windows;
using LeftoverScout.App.ViewModels;

namespace LeftoverScout.App;

/// <summary>
/// Main application window. Pure view code: all behavior lives in <see cref="MainViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Creates the window and binds it to its view model.</summary>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // A running cleanup is never cut off: the user is asked, and the window closes after the current item.
        Closing += (_, e) => e.Cancel = !viewModel.CanCloseNow();
        viewModel.CloseRequested += (_, _) => Close();
    }
}

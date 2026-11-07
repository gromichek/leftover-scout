using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LeftoverScout.App.ViewModels;

namespace LeftoverScout.App.Views;

/// <summary>
/// Shows a <see cref="ResultsPageViewModel"/>. The code-behind only forwards input events to commands.
/// </summary>
public partial class ResultsView : UserControl
{
    /// <summary>Creates the view.</summary>
    public ResultsView()
    {
        InitializeComponent();
    }

    private ResultsPageViewModel? ViewModel => DataContext as ResultsPageViewModel;

    // The DataGrid handles Enter and Ctrl+C itself, so these keys are forwarded before it sees them.
    private void FolderGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            viewModel.OpenFolderCommand.Execute(viewModel.SelectedRow);
            e.Handled = true;
        }
        else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (viewModel.CopyFolderPathCommand.CanExecute(null))
            {
                viewModel.CopyFolderPathCommand.Execute(null);
            }

            e.Handled = true;
        }
    }

    private void LargeFilesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel is { } viewModel && e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (viewModel.CopyFilePathCommand.CanExecute(null))
            {
                viewModel.CopyFilePathCommand.Execute(null);
            }

            e.Handled = true;
        }
    }

    private void FolderGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RowAt(sender, e) is { Item: FolderRowViewModel row } && ViewModel is { } viewModel)
        {
            viewModel.OpenFolderCommand.Execute(row);
            e.Handled = true;
        }
    }

    // A right click selects the row first, so the context menu acts on the row under the mouse.
    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (RowAt(sender, e) is { } row)
        {
            row.IsSelected = true;
        }
    }

    private static DataGridRow? RowAt(object sender, MouseButtonEventArgs e) =>
        sender is DataGrid grid && e.OriginalSource is DependencyObject source
            ? ItemsControl.ContainerFromElement(grid, source) as DataGridRow
            : null;
}

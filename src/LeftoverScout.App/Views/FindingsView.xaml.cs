using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LeftoverScout.App.ViewModels;

namespace LeftoverScout.App.Views;

/// <summary>
/// Shows a <see cref="FindingsPageViewModel"/>. The code-behind only forwards input events to commands and keeps the
/// default sort (Size, largest first) and its arrow in step.
/// </summary>
public partial class FindingsView : UserControl
{
    /// <summary>Creates the view.</summary>
    public FindingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is FindingsPageViewModel oldModel)
            {
                oldModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is FindingsPageViewModel newModel)
            {
                newModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
        FindingsGrid.Loaded += (_, _) => ApplyDefaultSort();
    }

    private FindingsPageViewModel? ViewModel => DataContext as FindingsPageViewModel;

    // New rows replace the old list: show the default order and its arrow again.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FindingsPageViewModel.Rows))
        {
            Dispatcher.BeginInvoke(new Action(ApplyDefaultSort));
        }
    }

    private void ApplyDefaultSort() =>
        DataGridSorting.ApplyDefault(FindingsGrid, FindingsPageViewModel.DefaultSortMember, FindingsPageViewModel.DefaultSortDirection);

    // The DataGrid handles Ctrl+C itself, so the key is forwarded before it sees it.
    private void FindingsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Space toggles the checkbox of the selected row (decision D-056).
        if (ViewModel is { } spaceModel && e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None
            && e.OriginalSource is not TextBox)
        {
            if (spaceModel.ToggleSelectedRowCommand.CanExecute(null))
            {
                spaceModel.ToggleSelectedRowCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        if (ViewModel is { } viewModel && e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (viewModel.CopyPathCommand.CanExecute(null))
            {
                viewModel.CopyPathCommand.Execute(null);
            }

            e.Handled = true;
        }
    }

    // A right click selects the row first, so the context menu acts on the row under the mouse.
    private void FindingsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid
            && e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(grid, source) is DataGridRow row)
        {
            row.IsSelected = true;
        }
    }
}

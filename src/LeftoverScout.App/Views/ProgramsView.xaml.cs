using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LeftoverScout.App.ViewModels;

namespace LeftoverScout.App.Views;

/// <summary>
/// Shows a <see cref="ProgramsPageViewModel"/>. The code-behind only forwards input events to commands and
/// keeps the default sort (by Name) and its arrow in step.
/// </summary>
public partial class ProgramsView : UserControl
{
    /// <summary>Creates the view.</summary>
    public ProgramsView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is ProgramsPageViewModel oldModel)
            {
                oldModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is ProgramsPageViewModel newModel)
            {
                newModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
        ProgramGrid.Loaded += (_, _) => ApplyDefaultSort();
    }

    private ProgramsPageViewModel? ViewModel => DataContext as ProgramsPageViewModel;

    // New rows replace the old list: show the default order and its arrow again (decision: default sort by Name).
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProgramsPageViewModel.Rows))
        {
            Dispatcher.BeginInvoke(new Action(ApplyDefaultSort));
        }
    }

    private void ApplyDefaultSort() => DataGridSorting.ApplyDefault(ProgramGrid, "Name", ListSortDirection.Ascending);

    private void ProgramGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ViewModel is { } viewModel && e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (viewModel.CopyNameCommand.CanExecute(null))
            {
                viewModel.CopyNameCommand.Execute(null);
            }

            e.Handled = true;
        }
    }

    // A right click selects the row first, so the context menu acts on the row under the mouse.
    private void ProgramGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid
            && e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(grid, source) is DataGridRow row)
        {
            row.IsSelected = true;
        }
    }
}

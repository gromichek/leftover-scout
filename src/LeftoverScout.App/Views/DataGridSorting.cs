using System.ComponentModel;
using System.Windows.Controls;

namespace LeftoverScout.App.Views;

/// <summary>
/// Pure view code: applies a default sort to a <see cref="DataGrid"/> so that the sort descriptions of the items
/// and the sort arrow of the column header agree.
/// </summary>
public static class DataGridSorting
{
    /// <summary>
    /// Sorts the grid by <paramref name="sortMemberPath"/> and shows the sort arrow on the column with that
    /// <see cref="DataGridColumn.SortMemberPath"/>; the arrow of every other column is cleared.
    /// </summary>
    public static void ApplyDefault(DataGrid grid, string sortMemberPath, ListSortDirection direction)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentException.ThrowIfNullOrEmpty(sortMemberPath);

        grid.Items.SortDescriptions.Clear();
        grid.Items.SortDescriptions.Add(new SortDescription(sortMemberPath, direction));

        foreach (DataGridColumn column in grid.Columns)
        {
            column.SortDirection = column.SortMemberPath == sortMemberPath ? direction : null;
        }
    }
}

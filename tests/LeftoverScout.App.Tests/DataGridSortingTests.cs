using System.ComponentModel;
using System.Windows.Controls;
using LeftoverScout.App.Tests.Help;
using LeftoverScout.App.ViewModels;
using LeftoverScout.App.Views;

namespace LeftoverScout.App.Tests;

public class DataGridSortingTests
{
    private static DataGrid CreateGrid() => new()
    {
        AutoGenerateColumns = false,
        Columns =
        {
            new DataGridTextColumn { Header = "Name", SortMemberPath = "Name" },
            new DataGridTextColumn { Header = "Size", SortMemberPath = "Size" },
        },
    };

    [Fact]
    public void ApplyDefault_SetsSortDescriptionAndArrowOnTheSameColumn()
    {
        (ListSortDirection? Name, ListSortDirection? Size, SortDescription Description) state = StaThread.Run(() =>
        {
            DataGrid grid = CreateGrid();
            DataGridSorting.ApplyDefault(grid, "Size", ListSortDirection.Descending);
            return (grid.Columns[0].SortDirection, grid.Columns[1].SortDirection, grid.Items.SortDescriptions.Single());
        });

        Assert.Null(state.Name);
        Assert.Equal(ListSortDirection.Descending, state.Size);
        Assert.Equal("Size", state.Description.PropertyName);
        Assert.Equal(ListSortDirection.Descending, state.Description.Direction);
    }

    [Fact]
    public void ApplyDefault_CalledTwice_ReplacesEarlierSort()
    {
        (ListSortDirection? Name, ListSortDirection? Size, int Count) state = StaThread.Run(() =>
        {
            DataGrid grid = CreateGrid();
            DataGridSorting.ApplyDefault(grid, "Size", ListSortDirection.Descending);
            DataGridSorting.ApplyDefault(grid, "Name", ListSortDirection.Ascending);
            return (grid.Columns[0].SortDirection, grid.Columns[1].SortDirection, grid.Items.SortDescriptions.Count);
        });

        Assert.Equal(ListSortDirection.Ascending, state.Name);
        Assert.Null(state.Size);
        Assert.Equal(1, state.Count);
    }

    [Fact]
    public void ResultsDefaultSort_IsSizeLargestFirst()
    {
        Assert.Equal("Size", ResultsPageViewModel.DefaultSortMember);
        Assert.Equal(ListSortDirection.Descending, ResultsPageViewModel.DefaultSortDirection);
    }

    [Fact]
    public void FolderRowViewModel_SizeColumnSortMember_ExistsOnTheRowViewModel()
    {
        // The Results grid sorts by "Size": the property must exist, otherwise the sort would silently do nothing.
        Assert.NotNull(typeof(FolderRowViewModel).GetProperty(ResultsPageViewModel.DefaultSortMember));
    }
}

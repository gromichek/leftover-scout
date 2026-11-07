using LeftoverScout.Core.Results;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Results;

public class FolderNavigatorTests
{
    private static FolderRow RowOf(FolderNavigator navigator, string name) =>
        FolderRowBuilder.Build(navigator.Current).Single(row => row.Name == name);

    [Fact]
    public void Constructor_StartsAtRootWithSingleBreadcrumbAndCannotGoUp()
    {
        var navigator = new FolderNavigator(ResultsTestTree.Sample().Root!);

        Assert.False(navigator.CanGoUp);
        BreadcrumbItem item = Assert.Single(navigator.Breadcrumb);
        Assert.Equal("SampleUser", item.Label);
        Assert.True(item.IsRoot);
        Assert.True(item.IsCurrent);
        Assert.False(item.ShowSeparator);
        Assert.False(navigator.GoUp());
    }

    [Fact]
    public void TryOpen_OpenableRow_MovesIntoFolderAndGrowsBreadcrumb()
    {
        var navigator = new FolderNavigator(ResultsTestTree.Sample().Root!);

        Assert.True(navigator.TryOpen(RowOf(navigator, "Big")));
        Assert.True(navigator.TryOpen(RowOf(navigator, "Deep")));

        Assert.Equal(new[] { "SampleUser", "Big", "Deep" }, navigator.Breadcrumb.Select(item => item.Label));
        Assert.Equal(new[] { false, false, true }, navigator.Breadcrumb.Select(item => item.IsCurrent));
        Assert.Equal(new[] { false, true, true }, navigator.Breadcrumb.Select(item => item.ShowSeparator));
        Assert.True(navigator.CanGoUp);
    }

    [Theory]
    [InlineData("Application Data")]
    [InlineData("OneDriveOnly")]
    [InlineData("Locked")]
    [InlineData("(files in this folder)")]
    public void TryOpen_RowThatCannotOpen_StaysInPlace(string name)
    {
        var navigator = new FolderNavigator(ResultsTestTree.Sample().Root!);
        FolderNode before = navigator.Current;

        Assert.False(navigator.TryOpen(RowOf(navigator, name)));

        Assert.Same(before, navigator.Current);
    }

    [Fact]
    public void GoUp_InsideFolder_ReturnsToParent()
    {
        var navigator = new FolderNavigator(ResultsTestTree.Sample().Root!);
        navigator.TryOpen(RowOf(navigator, "Big"));

        Assert.True(navigator.GoUp());

        Assert.Same(navigator.Root, navigator.Current);
        Assert.False(navigator.CanGoUp);
    }

    [Fact]
    public void NavigateTo_BreadcrumbPart_JumpsBack()
    {
        var navigator = new FolderNavigator(ResultsTestTree.Sample().Root!);
        navigator.TryOpen(RowOf(navigator, "Big"));
        navigator.TryOpen(RowOf(navigator, "Deep"));
        BreadcrumbItem big = navigator.Breadcrumb[1];

        Assert.True(navigator.NavigateTo(big.Node));
        Assert.Equal(new[] { "SampleUser", "Big" }, navigator.Breadcrumb.Select(item => item.Label));

        Assert.True(navigator.NavigateTo(navigator.Root));
        Assert.Same(navigator.Root, navigator.Current);
    }

    [Fact]
    public void NavigateTo_FolderOutsideBreadcrumb_IsRejected()
    {
        var navigator = new FolderNavigator(ResultsTestTree.Sample().Root!);
        FolderNode small = navigator.Root.Children.Single(child => child.Name == "Small");

        Assert.False(navigator.NavigateTo(small));
        Assert.Same(navigator.Root, navigator.Current);
    }
}

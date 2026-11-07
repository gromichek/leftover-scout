using LeftoverScout.Core.Results;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Results;

public class FolderRowBuilderTests
{
    [Fact]
    public void Build_Root_SortsBySizeDescendingThenName()
    {
        ScanResult result = ResultsTestTree.Sample();

        IReadOnlyList<FolderRow> rows = FolderRowBuilder.Build(result.Root!);

        // Big 700, Small 200, Documents 50 and the files row 50 (tie broken by name), then empty folders.
        Assert.Equal(new[] { "Big", "Small", "(files in this folder)", "Documents" }, rows.Take(4).Select(row => row.Name));
        Assert.Equal(7, rows.Count);
    }

    [Fact]
    public void Build_FolderWithOwnFiles_AddsFilesRowWithCurrentFolderPath()
    {
        ScanResult result = ResultsTestTree.Sample();
        FolderNode big = result.Root!.Children.Single(child => child.Name == "Big");

        FolderRow filesRow = Assert.Single(FolderRowBuilder.Build(big), row => row.IsFilesRow);

        Assert.Equal(FolderRowBuilder.FilesRowName, filesRow.Name);
        Assert.Equal(300, filesRow.LocalSize);
        Assert.Equal(1, filesRow.FileCount);
        Assert.Equal(big.FullPath, filesRow.FullPath);
        Assert.Null(filesRow.Node);
        Assert.False(filesRow.CanOpen);
        Assert.Equal(FolderTags.None, filesRow.Tags);
        Assert.Equal(300 * 100.0 / 700, filesRow.PercentOfParent, 6);
    }

    [Fact]
    public void Build_FolderWithoutOwnFiles_HidesFilesRow()
    {
        ScanResult result = ResultsTestTree.Sample();
        FolderNode deep = result.Root!.Children.Single(child => child.Name == "Big").Children.Single();

        // A folder whose files all live in subfolders has no files of its own.
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("OnlyFolders"))
            .Folder(Root + @"\OnlyFolders", FileEntry("x.bin", 5));
        ScanResult onlyFolders = ResultsTestTree.Scan(reader);

        Assert.DoesNotContain(FolderRowBuilder.Build(onlyFolders.Root!), row => row.IsFilesRow);
        Assert.Single(FolderRowBuilder.Build(deep), row => row.IsFilesRow);
    }

    [Fact]
    public void Build_EmptyFolder_HasNoRows()
    {
        ScanResult result = ResultsTestTree.Scan(new FakeDirectoryReader().Folder(Root));

        Assert.Empty(FolderRowBuilder.Build(result.Root!));
    }

    [Fact]
    public void Build_ZeroSizeParent_PercentIsZero()
    {
        // The only content is an empty subfolder, so the parent size is 0.
        ScanResult result = ResultsTestTree.Scan(new FakeDirectoryReader().Folder(Root, DirEntry("Empty")));

        FolderRow row = Assert.Single(FolderRowBuilder.Build(result.Root!));

        Assert.Equal(0, row.PercentOfParent);
    }

    [Fact]
    public void PercentOf_ZeroOrNegativeWhole_ReturnsZero()
    {
        Assert.Equal(0, FolderRowBuilder.PercentOf(10, 0));
        Assert.Equal(0, FolderRowBuilder.PercentOf(10, -1));
        Assert.Equal(25, FolderRowBuilder.PercentOf(1, 4));
        Assert.Equal(100, FolderRowBuilder.PercentOf(5, 4));
    }

    [Fact]
    public void Build_Subfolder_ReportsPercentFilesAndNewestDate()
    {
        ScanResult result = ResultsTestTree.Sample();

        FolderRow big = Assert.Single(FolderRowBuilder.Build(result.Root!), row => row.Name == "Big");

        Assert.Equal(700, big.LocalSize);
        Assert.Equal(2, big.FileCount);
        Assert.Equal(700 * 100.0 / 1000, big.PercentOfParent, 6);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), big.NewestWriteTimeUtc);
    }

    [Theory]
    [InlineData("Application Data", true, false, false)]
    [InlineData("OneDriveOnly", false, true, false)]
    [InlineData("Locked", false, false, true)]
    public void CanOpen_LinkCloudAndInaccessibleWithoutChildren_IsFalse(string name, bool link, bool cloud, bool inaccessible)
    {
        FolderNode node = ResultsTestTree.Sample().Root!.Children.Single(child => child.Name == name);

        Assert.Equal(link, node.IsSkippedLink);
        Assert.Equal(cloud, node.IsCloudFolderNotScanned);
        Assert.Equal(inaccessible, node.IsAccessDenied);
        Assert.False(FolderRowBuilder.CanOpen(node));
    }

    [Fact]
    public void CanOpen_NormalFolderAndPersonalFolder_IsTrue()
    {
        ScanResult result = ResultsTestTree.Sample();

        Assert.All(
            result.Root!.Children.Where(child => child.Name is "Big" or "Small" or "Documents"),
            node => Assert.True(FolderRowBuilder.CanOpen(node)));
    }

    [Fact]
    public void CanOpen_FolderWithUnreadableChild_ParentOpensChildDoesNot()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Parent"))
            .Folder(Root + @"\Parent", DirEntry("Child"), FileEntry("p.bin", 1))
            .Failing(Root + @"\Parent\Child", "Broken", accessDenied: false);
        ScanResult result = ResultsTestTree.Scan(reader);
        FolderNode child = result.Root!.Children.Single().Children.Single();

        Assert.True(child.HasError);
        Assert.False(FolderRowBuilder.CanOpen(child));
        Assert.True(FolderRowBuilder.CanOpen(result.Root.Children.Single()));
    }

    [Fact]
    public void CanOpen_InaccessibleFolderThatHasChildren_IsTrue()
    {
        var node = new FolderNode("Partial", Root + @"\Partial", parent: null) { IsAccessDenied = true };
        node.MutableChildren.Add(new FolderNode("Inner", Root + @"\Partial\Inner", node));

        Assert.True(FolderRowBuilder.CanOpen(node));
    }

    [Fact]
    public void Build_Rows_CarryTagsAndOpenability()
    {
        ScanResult result = ResultsTestTree.Sample();

        IReadOnlyList<FolderRow> rows = FolderRowBuilder.Build(result.Root!);

        Assert.Equal(FolderTags.Personal, rows.Single(row => row.Name == "Documents").Tags);
        Assert.Equal(FolderTags.Link, rows.Single(row => row.Name == "Application Data").Tags);
        Assert.Equal(FolderTags.Cloud, rows.Single(row => row.Name == "OneDriveOnly").Tags);
        Assert.Equal(FolderTags.Inaccessible, rows.Single(row => row.Name == "Locked").Tags);
        Assert.False(rows.Single(row => row.Name == "Locked").CanOpen);
        Assert.True(rows.Single(row => row.Name == "Big").CanOpen);
    }
}

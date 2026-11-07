using LeftoverScout.Core.Results;
using LeftoverScout.Core.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Results;

public class FolderTagTextTests
{
    [Theory]
    [InlineData(FolderTags.None, "")]
    [InlineData(FolderTags.Personal, "Personal")]
    [InlineData(FolderTags.Link, "Link")]
    [InlineData(FolderTags.Cloud, "Cloud")]
    [InlineData(FolderTags.Inaccessible, "Inaccessible")]
    [InlineData(FolderTags.Personal | FolderTags.Inaccessible, "Personal, Inaccessible")]
    [InlineData(FolderTags.Personal | FolderTags.Link | FolderTags.Cloud | FolderTags.Inaccessible, "Personal, Link, Cloud, Inaccessible")]
    public void Describe_Tags_JoinsNamesInFixedOrder(FolderTags tags, string expected)
    {
        Assert.Equal(expected, FolderTagText.Describe(tags));
    }

    [Fact]
    public void Describe_EveryFlagCombination_NamesMatchTheFlags()
    {
        for (int bits = 0; bits < 16; bits++)
        {
            var tags = (FolderTags)bits;
            string text = FolderTagText.Describe(tags);
            string[] names = text.Length == 0 ? [] : text.Split(", ");

            Assert.Equal(int.PopCount(bits), names.Length);
            Assert.Equal(tags.HasFlag(FolderTags.Personal), names.Contains("Personal"));
            Assert.Equal(tags.HasFlag(FolderTags.Link), names.Contains("Link"));
            Assert.Equal(tags.HasFlag(FolderTags.Cloud), names.Contains("Cloud"));
            Assert.Equal(tags.HasFlag(FolderTags.Inaccessible), names.Contains("Inaccessible"));
        }
    }

    [Fact]
    public void Explain_Tags_HasOneLinePerTag()
    {
        Assert.Equal(string.Empty, FolderTagText.Explain(FolderTags.None));
        Assert.Single(FolderTagText.Explain(FolderTags.Link).Split('\n'));
        Assert.Equal(4, FolderTagText.Explain(FolderTags.Personal | FolderTags.Link | FolderTags.Cloud | FolderTags.Inaccessible).Split('\n').Length);
        Assert.StartsWith("Personal:", FolderTagText.Explain(FolderTags.Personal));
    }

    [Fact]
    public void FromNode_AccessDeniedAndOtherError_BothMeanInaccessible()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Denied"), DirEntry("Broken"))
            .Failing(Root + @"\Denied", "Access denied", accessDenied: true)
            .Failing(Root + @"\Broken", "Some error", accessDenied: false);
        ScanResult result = ResultsTestTree.Scan(reader);

        Assert.All(result.Root!.Children, node => Assert.Equal(FolderTags.Inaccessible, FolderTagText.FromNode(node)));
    }
}

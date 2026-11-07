using LeftoverScout.Core.Results;
using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Results;

public class LargeFileRowBuilderTests
{
    private const string ProfileRoot = @"C:\Users\SampleUser";

    [Fact]
    public void Build_Files_AreRelativeToRootAndSortedBySize()
    {
        var files = new[]
        {
            new LargeFile(ProfileRoot + @"\Videos\small.mkv", 100, DateTime.UnixEpoch),
            new LargeFile(ProfileRoot + @"\AppData\Local\cache.bin", 900, DateTime.UnixEpoch),
            new LargeFile(ProfileRoot + @"\a.iso", 100, DateTime.UnixEpoch),
        };

        IReadOnlyList<LargeFileRow> rows = LargeFileRowBuilder.Build(files, ProfileRoot);

        Assert.Equal(new[] { @"AppData\Local\cache.bin", "a.iso", @"Videos\small.mkv" }, rows.Select(row => row.RelativePath));
        Assert.Equal(ProfileRoot + @"\AppData\Local\cache.bin", rows[0].FullPath);
        Assert.Equal(900, rows[0].Size);
    }

    [Fact]
    public void Build_NoFiles_IsEmpty()
    {
        Assert.Empty(LargeFileRowBuilder.Build([], ProfileRoot));
    }

    [Theory]
    [InlineData(@"C:\Users\SampleUser\x.bin", @"C:\Users\SampleUser", "x.bin")]
    [InlineData(@"C:\Users\SampleUser\x.bin", @"C:\Users\SampleUser\", "x.bin")]
    [InlineData(@"c:\users\sampleuser\A\x.bin", @"C:\Users\SampleUser", @"A\x.bin")]
    [InlineData(@"C:\Users\SampleUser2\x.bin", @"C:\Users\SampleUser", @"C:\Users\SampleUser2\x.bin")]
    [InlineData(@"D:\Other\x.bin", @"C:\Users\SampleUser", @"D:\Other\x.bin")]
    public void RelativePath_Variants_StripsOnlyARealRootPrefix(string fullPath, string root, string expected)
    {
        Assert.Equal(expected, LargeFileRowBuilder.RelativePath(fullPath, root));
    }

    [Fact]
    public void Build_ScanResult_UsesLargeFilesOfTheScan()
    {
        ScanResult result = ResultsTestTree.Sample();

        IReadOnlyList<LargeFileRow> rows = LargeFileRowBuilder.Build(result.LargeFiles, result.Root!.FullPath);

        Assert.Equal(new[] { @"Big\Deep\deep.bin", @"Big\big.bin" }, rows.Select(row => row.RelativePath));
    }
}

using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Tests.Scanning;

public class EntryClassifierTests
{
    private const uint Directory = EntryClassifier.AttributeDirectory;
    private const uint Reparse = EntryClassifier.AttributeReparsePoint;

    private static DirectoryEntry Entry(bool isDirectory, uint attributes, uint tag) =>
        new("Sample", isDirectory, 10, DateTime.UnixEpoch, attributes, tag);

    [Theory]
    [InlineData(EntryClassifier.ReparseTagMountPoint)]
    [InlineData(EntryClassifier.ReparseTagSymlink)]
    [InlineData(0xA000001Du)] // another name-surrogate tag
    public void Classify_NameSurrogateFolder_IsSkippedLink(uint tag)
    {
        Assert.Equal(EntryKind.SkippedLink, EntryClassifier.Classify(Entry(true, Directory | Reparse, tag)));
    }

    [Fact]
    public void Classify_NameSurrogateFile_IsSkippedLink()
    {
        Assert.Equal(EntryKind.SkippedLink, EntryClassifier.Classify(Entry(false, Reparse, EntryClassifier.ReparseTagSymlink)));
    }

    [Fact]
    public void Classify_TagWithoutReparseAttribute_IsIgnored()
    {
        Assert.Equal(EntryKind.Directory, EntryClassifier.Classify(Entry(true, Directory, EntryClassifier.ReparseTagMountPoint)));
    }

    [Theory]
    [InlineData(0x9000001Au)]
    [InlineData(0x9000101Au)]
    [InlineData(0x9000701Au)]
    [InlineData(0x9000F01Au)]
    public void IsCloudTag_CloudFamily_ReturnsTrue(uint tag)
    {
        Assert.True(EntryClassifier.IsCloudTag(tag));
        Assert.False(EntryClassifier.IsNameSurrogate(tag));
    }

    [Theory]
    [InlineData(0x9000001Bu)]
    [InlineData(EntryClassifier.ReparseTagAppExecLink)]
    [InlineData(EntryClassifier.ReparseTagMountPoint)]
    public void IsCloudTag_OtherTags_ReturnsFalse(uint tag)
    {
        Assert.False(EntryClassifier.IsCloudTag(tag));
    }

    [Fact]
    public void Classify_CloudPlaceholderFolder_IsScannedAsNormalFolder()
    {
        Assert.Equal(EntryKind.Directory, EntryClassifier.Classify(Entry(true, Directory | Reparse, 0x9000701A)));
    }

    [Fact]
    public void Classify_CloudPlaceholderFileWithLocalData_IsNormalFile()
    {
        Assert.Equal(EntryKind.File, EntryClassifier.Classify(Entry(false, Reparse, 0x9000701A)));
    }

    [Fact]
    public void Classify_FileRecallOnDataAccess_IsCloudOnly()
    {
        Assert.Equal(EntryKind.CloudOnlyFile, EntryClassifier.Classify(Entry(false, Reparse | EntryClassifier.AttributeRecallOnDataAccess, 0x9000701A)));
    }

    [Fact]
    public void Classify_FileOffline_IsCloudOnly()
    {
        Assert.Equal(EntryKind.CloudOnlyFile, EntryClassifier.Classify(Entry(false, EntryClassifier.AttributeOffline, 0)));
    }

    [Fact]
    public void Classify_FolderRecallOnOpen_IsCloudFolderNotScanned()
    {
        Assert.Equal(EntryKind.CloudFolderNotScanned, EntryClassifier.Classify(Entry(true, Directory | Reparse | EntryClassifier.AttributeRecallOnOpen, 0x9000701A)));
    }

    [Fact]
    public void Classify_FolderRecallOnDataAccessOnly_IsStillScanned()
    {
        // Rule 3 applies to files only.
        Assert.Equal(EntryKind.Directory, EntryClassifier.Classify(Entry(true, Directory | EntryClassifier.AttributeRecallOnDataAccess, 0)));
    }

    [Fact]
    public void Classify_AppExecLinkFile_IsCountedAsFile()
    {
        Assert.Equal(EntryKind.File, EntryClassifier.Classify(Entry(false, Reparse, EntryClassifier.ReparseTagAppExecLink)));
    }

    [Fact]
    public void Classify_PlainEntries_AreFileAndDirectory()
    {
        Assert.Equal(EntryKind.File, EntryClassifier.Classify(Entry(false, 0x20, 0)));
        Assert.Equal(EntryKind.Directory, EntryClassifier.Classify(Entry(true, Directory, 0)));
    }
}

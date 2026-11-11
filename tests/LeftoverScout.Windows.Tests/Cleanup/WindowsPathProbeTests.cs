using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Windows.Cleanup;
using LeftoverScout.Windows.Tests.Scanning;

namespace LeftoverScout.Windows.Tests.Cleanup;

public class WindowsPathProbeTests
{
    private readonly WindowsPathProbe _probe = new();

    [Fact]
    public void Probe_ExistingFolder_IsFoundAsDirectory()
    {
        using var temp = new WindowsTempFolder();

        PathProbe result = _probe.Probe(temp.Path);

        Assert.Equal(PathProbeStatus.Found, result.Status);
        Assert.NotEqual(0u, result.Attributes & EntryClassifier.AttributeDirectory);
        Assert.Equal(0u, result.ReparseTag);
    }

    [Fact]
    public void Probe_TrailingSeparator_StillDescribesTheFolderItself()
    {
        using var temp = new WindowsTempFolder();

        PathProbe result = _probe.Probe(temp.Path + @"\");

        Assert.Equal(PathProbeStatus.Found, result.Status);
        Assert.NotEqual(0u, result.Attributes & EntryClassifier.AttributeDirectory);
    }

    [Fact]
    public void Probe_File_IsFoundWithoutDirectoryAttribute()
    {
        using var temp = new WindowsTempFolder();
        string file = Path.Combine(temp.Path, "data.bin");
        File.WriteAllBytes(file, new byte[10]);

        PathProbe result = _probe.Probe(file);

        Assert.Equal(PathProbeStatus.Found, result.Status);
        Assert.Equal(0u, result.Attributes & EntryClassifier.AttributeDirectory);
    }

    [Fact]
    public void Probe_MissingPathAndMissingParent_AreNotFound()
    {
        using var temp = new WindowsTempFolder();

        Assert.Equal(PathProbeStatus.NotFound, _probe.Probe(Path.Combine(temp.Path, "missing")).Status);
        Assert.Equal(PathProbeStatus.NotFound, _probe.Probe(Path.Combine(temp.Path, "missing", "child")).Status);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("?")]
    public void Probe_Wildcard_IsNeverSearched(string pattern)
    {
        using var temp = new WindowsTempFolder();
        Directory.CreateDirectory(Path.Combine(temp.Path, "Sub"));

        Assert.Equal(PathProbeStatus.Failed, _probe.Probe(Path.Combine(temp.Path, pattern)).Status);
    }

    [Fact]
    public void Probe_Junction_ReportsTheLinkAndDoesNotFollowIt()
    {
        using var temp = new WindowsTempFolder();
        string target = Path.Combine(temp.Path, "target");
        string link = Path.Combine(temp.Path, "link");
        Directory.CreateDirectory(target);
        if (!JunctionHelper.TryCreate(link, target))
        {
            Assert.Skip("A directory junction could not be created in this environment.");
        }

        try
        {
            PathProbe result = _probe.Probe(link);

            Assert.Equal(PathProbeStatus.Found, result.Status);
            Assert.NotEqual(0u, result.Attributes & EntryClassifier.AttributeReparsePoint);
            Assert.Equal(EntryClassifier.ReparseTagMountPoint, result.ReparseTag);
            Assert.True(EntryClassifier.IsNameSurrogate(result.ReparseTag));
        }
        finally
        {
            JunctionHelper.Remove(link);
        }
    }

    [Fact]
    public void Probe_PathLongerThan260Characters_Works()
    {
        using var temp = new WindowsTempFolder();
        string deep = temp.Path;
        for (int i = 0; i < 6; i++)
        {
            deep = Path.Combine(deep, new string((char)('a' + i), 50));
        }

        Directory.CreateDirectory(deep);
        Assert.True(deep.Length > 260);

        Assert.Equal(PathProbeStatus.Found, _probe.Probe(deep).Status);
        Assert.EndsWith(new string('f', 50), _probe.GetLongPath(deep), StringComparison.Ordinal);
    }

    [Fact]
    public void GetLongPath_OrdinaryFolder_ReturnsTheSamePathWithoutPrefix()
    {
        using var temp = new WindowsTempFolder();
        string folder = Path.Combine(temp.Path, "Some Folder");
        Directory.CreateDirectory(folder);

        string? longPath = _probe.GetLongPath(folder);

        Assert.NotNull(longPath);
        Assert.DoesNotContain(@"\\?\", longPath, StringComparison.Ordinal);
        Assert.Equal(_probe.GetLongPath(temp.Path) + @"\Some Folder", longPath, ignoreCase: true);
    }

    [Fact]
    public void GetLongPath_WrongLetterCase_ReturnsTheRealCase()
    {
        using var temp = new WindowsTempFolder();
        string folder = Path.Combine(temp.Path, "MixedCaseName");
        Directory.CreateDirectory(folder);

        string? longPath = _probe.GetLongPath(Path.Combine(temp.Path, "MIXEDCASENAME"));

        Assert.NotNull(longPath);
        Assert.EndsWith(@"\MixedCaseName", longPath, StringComparison.Ordinal);
    }

    [Fact]
    public void GetLongPath_ShortName_ReturnsTheLongName()
    {
        using var temp = new WindowsTempFolder();
        string folder = Path.Combine(temp.Path, "A Rather Long Folder Name");
        Directory.CreateDirectory(folder);
        string? shortLeaf = ShortPathHelper.GetShortLeafName(folder);
        if (shortLeaf is null)
        {
            Assert.Skip("8.3 short names are disabled on this volume.");
        }

        string? longPath = _probe.GetLongPath(Path.Combine(temp.Path, shortLeaf));

        Assert.NotNull(longPath);
        Assert.EndsWith(@"\A Rather Long Folder Name", longPath, StringComparison.Ordinal);
    }

    [Fact]
    public void GetLongPath_MissingPath_ReturnsNull()
    {
        using var temp = new WindowsTempFolder();

        Assert.Null(_probe.GetLongPath(Path.Combine(temp.Path, "missing")));
    }
}

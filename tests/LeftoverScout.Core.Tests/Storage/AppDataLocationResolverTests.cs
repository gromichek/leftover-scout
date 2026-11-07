using LeftoverScout.Core.Storage;

namespace LeftoverScout.Core.Tests.Storage;

public class AppDataLocationResolverTests
{
    private const string ProgramFolder = @"C:\Apps\SampleApp";
    private const string LocalAppData = @"C:\Users\SampleUser\AppData\Local";

    [Fact]
    public void Resolve_ProgramFolderWritable_UsesDataFolderNextToProgram()
    {
        var probe = new FakeProbe(_ => true);

        AppDataLocation result = new AppDataLocationResolver(probe).Resolve(ProgramFolder, LocalAppData);

        Assert.Equal(Path.Combine(ProgramFolder, "data"), result.Path);
        Assert.False(result.IsFallback);
        Assert.Single(probe.Probed);
    }

    [Fact]
    public void Resolve_ProgramFolderNotWritable_UsesLocalAppDataFallback()
    {
        string portable = Path.Combine(ProgramFolder, "data");
        var probe = new FakeProbe(path => path != portable);

        AppDataLocation result = new AppDataLocationResolver(probe).Resolve(ProgramFolder, LocalAppData);

        Assert.Equal(Path.Combine(LocalAppData, "LeftoverScout"), result.Path);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public void Resolve_FolderCannotBeCreatedAndFallbackFails_StillReturnsFallback()
    {
        var probe = new FakeProbe(_ => false);

        AppDataLocation result = new AppDataLocationResolver(probe).Resolve(ProgramFolder, LocalAppData);

        Assert.True(result.IsFallback);
        Assert.Equal(Path.Combine(LocalAppData, "LeftoverScout"), result.Path);
    }

    private sealed class FakeProbe : IDataFolderProbe
    {
        private readonly Func<string, bool> _result;

        public FakeProbe(Func<string, bool> result)
        {
            _result = result;
        }

        public List<string> Probed { get; } = [];

        public bool TryEnsureWritable(string folderPath)
        {
            Probed.Add(folderPath);
            return _result(folderPath);
        }
    }
}

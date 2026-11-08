using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Evidence;

public class ProgramFolderSourceTests
{
    private const string ProgramFiles = @"C:\Program Files";
    private const string ProgramFilesX86 = @"C:\Program Files (x86)";
    private const string LocalPrograms = Root + @"\AppData\Local\Programs";

    private static IReadOnlyList<InstalledProgram> Collect(FakeDirectoryReader reader, params string[] roots) =>
        new ProgramFolderSource(reader, new FakeEvidenceLocations(programFolders: roots)).Collect(CancellationToken.None);

    [Fact]
    public void Collect_ListsTopLevelFoldersWithSubfolderNames()
    {
        var reader = new FakeDirectoryReader()
            .Folder(ProgramFiles, DirEntry("Sample Suite"), FileEntry("readme.txt", 5))
            .Folder(ProgramFiles + @"\Sample Suite", DirEntry("bin"), DirEntry("Plugins"), FileEntry("app.exe", 10));

        InstalledProgram program = Assert.Single(Collect(reader, ProgramFiles));

        Assert.Equal("Sample Suite", program.Name);
        Assert.Equal(ProgramSource.ProgramFolder, program.Source);
        Assert.Equal(ProgramFiles + @"\Sample Suite", program.SourceDetail);
        Assert.Equal(ProgramFiles + @"\Sample Suite", program.InstallLocation);
        Assert.Equal(["bin", "Plugins"], program.SubfolderNames);
        Assert.False(program.IsSystemComponent);
    }

    [Fact]
    public void Collect_ReadsAllRoots_AndSkipsMissingRoots()
    {
        var reader = new FakeDirectoryReader()
            .Folder(ProgramFiles, DirEntry("App A"))
            .Folder(ProgramFilesX86, DirEntry("App B"))
            .Failing(LocalPrograms, "not found", accessDenied: false);

        IReadOnlyList<InstalledProgram> programs = Collect(reader, ProgramFiles, ProgramFilesX86, LocalPrograms);

        Assert.Equal(["App A", "App B"], programs.Select(p => p.Name));
    }

    [Fact]
    public void Collect_SameRootTwice_IsReadOnce()
    {
        var reader = new FakeDirectoryReader().Folder(ProgramFiles, DirEntry("App A"));

        IReadOnlyList<InstalledProgram> programs = Collect(reader, ProgramFiles, ProgramFiles.ToUpperInvariant());

        Assert.Single(programs);
    }

    [Fact]
    public void Collect_IgnoresReparsePointsAndCloudFolders()
    {
        var reader = new FakeDirectoryReader()
            .Folder(ProgramFiles, DirEntry("Real App"), JunctionEntry("Linked App"), CloudDirEntry("Cloud App"))
            .Folder(ProgramFiles + @"\Real App", DirEntry("data"), JunctionEntry("link"), CloudDirEntry("cloud"));

        InstalledProgram program = Assert.Single(Collect(reader, ProgramFiles));

        Assert.Equal("Real App", program.Name);
        Assert.Equal(["data"], program.SubfolderNames);
        Assert.DoesNotContain(ProgramFiles + @"\Linked App", reader.ReadPaths);
        Assert.DoesNotContain(ProgramFiles + @"\Cloud App", reader.ReadPaths);
    }

    [Fact]
    public void Collect_Cancelled_Throws()
    {
        var reader = new FakeDirectoryReader().Folder(ProgramFiles, DirEntry("App A"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => new ProgramFolderSource(reader, new FakeEvidenceLocations(programFolders: [ProgramFiles])).Collect(cts.Token));
    }
}

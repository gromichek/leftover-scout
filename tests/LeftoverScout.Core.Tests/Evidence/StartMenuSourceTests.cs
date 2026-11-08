using LeftoverScout.Core.Evidence;
using LeftoverScout.Core.Tests.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Evidence;

public class StartMenuSourceTests
{
    private const string UserMenu = Root + @"\AppData\Roaming\Microsoft\Windows\Start Menu\Programs";
    private const string AllUsersMenu = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs";

    private static IReadOnlyList<InstalledProgram> Collect(FakeDirectoryReader reader, params string[] roots) =>
        new StartMenuSource(reader, new FakeEvidenceLocations(startMenu: roots)).Collect(CancellationToken.None);

    [Fact]
    public void Collect_ShortcutsAreListedWithoutExtension_OtherFilesAreIgnored()
    {
        var reader = new FakeDirectoryReader().Folder(
            UserMenu,
            FileEntry("Sample App.lnk", 1),
            FileEntry("Sample Site.url", 1),
            FileEntry("UPPER.LNK", 1),
            FileEntry("notes.txt", 1),
            FileEntry("tool.exe", 1));

        IReadOnlyList<InstalledProgram> programs = Collect(reader, UserMenu);

        Assert.Equal(["Sample App", "Sample Site", "UPPER"], programs.Select(p => p.Name).Order());
        Assert.All(programs, p => Assert.Equal(ProgramSource.StartMenu, p.Source));
    }

    [Fact]
    public void Collect_NestedFolders_AreRecursedAndDetailIsRelativeToRoot()
    {
        var reader = new FakeDirectoryReader()
            .Folder(UserMenu, DirEntry("Sample Suite"))
            .Folder(UserMenu + @"\Sample Suite", FileEntry("Sample Editor.lnk", 1), DirEntry("Tools"))
            .Folder(UserMenu + @"\Sample Suite\Tools", FileEntry("Sample Helper.lnk", 1));

        IReadOnlyList<InstalledProgram> programs = Collect(reader, UserMenu);

        Assert.Equal(["Sample Editor", "Sample Helper", "Sample Suite", "Tools"], programs.Select(p => p.Name).Order());
        Assert.Equal(@"Sample Suite\Tools\Sample Helper.lnk", programs.Single(p => p.Name == "Sample Helper").SourceDetail);
        Assert.Equal(@"Sample Suite\Tools", programs.Single(p => p.Name == "Tools").SourceDetail);
        Assert.All(programs, p => Assert.Null(p.InstallLocation));
    }

    [Fact]
    public void Collect_FolderWithSameNameAsShortcut_IsNotListedTwice()
    {
        var reader = new FakeDirectoryReader()
            .Folder(UserMenu, DirEntry("Sample App"), FileEntry("sample app.lnk", 1));

        InstalledProgram program = Assert.Single(Collect(reader, UserMenu));

        Assert.Equal("sample app", program.Name);
        Assert.EndsWith(".lnk", program.SourceDetail, StringComparison.Ordinal);
    }

    [Fact]
    public void Collect_ShortcutInSubfolderSameNameAsTopFolder_HidesTheFolder()
    {
        var reader = new FakeDirectoryReader()
            .Folder(UserMenu, DirEntry("Sample App"))
            .Folder(UserMenu + @"\Sample App", FileEntry("Sample App.lnk", 1));

        InstalledProgram program = Assert.Single(Collect(reader, UserMenu));

        Assert.Equal(@"Sample App\Sample App.lnk", program.SourceDetail);
    }

    [Fact]
    public void Collect_UserAndAllUsersMenus_AreBothRead()
    {
        var reader = new FakeDirectoryReader()
            .Folder(UserMenu, FileEntry("User App.lnk", 1))
            .Folder(AllUsersMenu, FileEntry("Shared App.lnk", 1));

        IReadOnlyList<InstalledProgram> programs = Collect(reader, UserMenu, AllUsersMenu);

        Assert.Equal(["Shared App", "User App"], programs.Select(p => p.Name).Order());
    }

    [Fact]
    public void Collect_ReparsePointFolders_AreIgnored()
    {
        var reader = new FakeDirectoryReader()
            .Folder(UserMenu, JunctionEntry("Linked"), CloudDirEntry("Cloud"), FileEntry("Real App.lnk", 1))
            .Folder(UserMenu + @"\Linked", FileEntry("Hidden App.lnk", 1));

        IReadOnlyList<InstalledProgram> programs = Collect(reader, UserMenu);

        Assert.Equal(["Real App"], programs.Select(p => p.Name));
        Assert.DoesNotContain(UserMenu + @"\Linked", reader.ReadPaths);
        Assert.DoesNotContain(UserMenu + @"\Cloud", reader.ReadPaths);
    }

    [Fact]
    public void Collect_OnlyListsFolders_NeverOpensFiles()
    {
        var reader = new FakeDirectoryReader().Folder(UserMenu, FileEntry("Sample App.lnk", 1));

        Collect(reader, UserMenu);

        // The only thing ever read is the folder listing.
        Assert.Equal([UserMenu], reader.ReadPaths);
    }

    [Fact]
    public void Collect_Cancelled_Throws()
    {
        var reader = new FakeDirectoryReader().Folder(UserMenu, FileEntry("Sample App.lnk", 1));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => new StartMenuSource(reader, new FakeEvidenceLocations(startMenu: [UserMenu])).Collect(cts.Token));
    }
}

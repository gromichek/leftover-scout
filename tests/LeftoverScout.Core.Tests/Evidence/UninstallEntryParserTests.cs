using LeftoverScout.Core.Evidence;

namespace LeftoverScout.Core.Tests.Evidence;

public class UninstallEntryParserTests
{
    private static readonly UninstallLocation Hklm64 = new(UninstallHive.LocalMachine, UninstallView.Registry64);
    private static readonly UninstallLocation Hklm32 = new(UninstallHive.LocalMachine, UninstallView.Registry32);
    private static readonly UninstallLocation Hkcu64 = new(UninstallHive.CurrentUser, UninstallView.Registry64);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NoDisplayName_ReturnsNull(string? displayName)
    {
        Assert.Null(UninstallEntryParser.Parse(Hklm64, new UninstallEntry("Key", displayName)));
    }

    [Fact]
    public void Parse_ParentKeyNameSet_IsSkippedAsUpdate()
    {
        var entry = new UninstallEntry("KB1", "Sample Patch", ParentKeyName: "SampleApp");

        Assert.Null(UninstallEntryParser.Parse(Hklm64, entry));
    }

    [Theory]
    [InlineData("Update")]
    [InlineData("Hotfix")]
    [InlineData("Security Update")]
    [InlineData("security update")]
    public void Parse_UpdateReleaseType_IsSkipped(string releaseType)
    {
        var entry = new UninstallEntry("KB2", "Sample Update", ReleaseType: releaseType);

        Assert.Null(UninstallEntryParser.Parse(Hklm64, entry));
    }

    [Fact]
    public void Parse_OtherReleaseType_IsKept()
    {
        var entry = new UninstallEntry("App", "Sample App", ReleaseType: "Application");

        Assert.NotNull(UninstallEntryParser.Parse(Hklm64, entry));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(null, false)]
    public void Parse_SystemComponent_SetsFlagOnlyForOne(int? value, bool expected)
    {
        var entry = new UninstallEntry("App", "Sample App", SystemComponent: value);

        Assert.Equal(expected, UninstallEntryParser.Parse(Hklm64, entry)!.IsSystemComponent);
    }

    [Theory]
    [InlineData("20240315", 2024, 3, 15)]
    [InlineData(" 20240315 ", 2024, 3, 15)]
    public void ParseInstallDate_ValidText_ReturnsDate(string text, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), UninstallEntryParser.ParseInstallDate(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2024-03-15")]
    [InlineData("15/03/2024")]
    [InlineData("20241340")]
    [InlineData("2024031")]
    [InlineData("abc")]
    public void ParseInstallDate_AnythingElse_ReturnsNull(string? text)
    {
        Assert.Null(UninstallEntryParser.ParseInstallDate(text));
    }

    [Fact]
    public void Parse_FillsAllFieldsAndSourceDetail()
    {
        var entry = new UninstallEntry(
            "SampleApp",
            "  Sample App  ",
            Publisher: "Sample Publisher",
            DisplayVersion: "1.2.3",
            InstallLocation: @"C:\Program Files\Sample App",
            InstallDate: "20240315");

        InstalledProgram program = UninstallEntryParser.Parse(Hklm64, entry)!;

        Assert.Equal("Sample App", program.Name);
        Assert.Equal(ProgramSource.Registry, program.Source);
        Assert.Equal("Sample Publisher", program.Publisher);
        Assert.Equal("1.2.3", program.Version);
        Assert.Equal(@"C:\Program Files\Sample App", program.InstallLocation);
        Assert.Equal(new DateOnly(2024, 3, 15), program.InstallDate);
        Assert.Equal(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\SampleApp (64-bit view)", program.SourceDetail);
    }

    [Fact]
    public void Parse_EmptyInstallLocation_UsesFolderOfDisplayIcon()
    {
        var entry = new UninstallEntry("App", "Sample App", InstallLocation: " ", DisplayIcon: "\"C:\\Apps\\Sample\\sample.exe\",0");

        Assert.Equal(@"C:\Apps\Sample", UninstallEntryParser.Parse(Hklm64, entry)!.InstallLocation);
    }

    [Fact]
    public void Parse_InstallLocationPresent_IgnoresDisplayIcon()
    {
        var entry = new UninstallEntry("App", "Sample App", InstallLocation: @"C:\Apps\Real", DisplayIcon: @"C:\Other\icon.ico");

        Assert.Equal(@"C:\Apps\Real", UninstallEntryParser.Parse(Hklm64, entry)!.InstallLocation);
    }

    [Theory]
    [InlineData("\"C:\\Apps\\Sample\\sample.exe\",0", @"C:\Apps\Sample")]
    [InlineData("\"C:\\Apps\\Sample\\sample.exe\"", @"C:\Apps\Sample")]
    [InlineData(@"C:\Apps\Sample\sample.exe,-101", @"C:\Apps\Sample")]
    [InlineData(@"C:\Apps\Sample\sample.exe,2", @"C:\Apps\Sample")]
    [InlineData(@"C:\Apps\Sample\sample.exe", @"C:\Apps\Sample")]
    [InlineData("\"C:\\Program Files\\Sample, Inc\\app.exe\",1", @"C:\Program Files\Sample, Inc")]
    [InlineData("sample.exe", null)]
    [InlineData(@"C:\sample.exe", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void FolderOfIcon_Examples(string? icon, string? expected)
    {
        Assert.Equal(expected, UninstallEntryParser.FolderOfIcon(icon));
    }

    [Fact]
    public void ParseAll_SameProgramInBothViews_IsListedOnce()
    {
        var entry = new UninstallEntry("SampleApp", "Sample App");

        IReadOnlyList<InstalledProgram> programs = UninstallEntryParser.ParseAll(
        [
            (Hklm64, [entry]),
            (Hklm32, [entry]),
        ]);

        InstalledProgram only = Assert.Single(programs);
        Assert.Contains("64-bit view", only.SourceDetail, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseAll_SameKeyNameButDifferentDisplayName_IsListedTwice()
    {
        IReadOnlyList<InstalledProgram> programs = UninstallEntryParser.ParseAll(
        [
            (Hklm64, [new UninstallEntry("Key", "Sample App")]),
            (Hklm32, [new UninstallEntry("Key", "Other App")]),
        ]);

        Assert.Equal(2, programs.Count);
    }

    [Fact]
    public void ParseAll_SameProgramInDifferentHives_IsListedTwice()
    {
        var entry = new UninstallEntry("SampleApp", "Sample App");

        IReadOnlyList<InstalledProgram> programs = UninstallEntryParser.ParseAll(
        [
            (Hklm64, [entry]),
            (Hkcu64, [entry]),
        ]);

        Assert.Equal(2, programs.Count);
    }

    [Fact]
    public void ParseAll_SkipsNonPrograms()
    {
        IReadOnlyList<InstalledProgram> programs = UninstallEntryParser.ParseAll(
        [
            (Hklm64, [new UninstallEntry("A", "App A"), new UninstallEntry("B", null), new UninstallEntry("C", "Patch", ParentKeyName: "A")]),
        ]);

        Assert.Equal(["App A"], programs.Select(p => p.Name));
    }

    [Fact]
    public void Collect_ReadsAllFourLocationsWithTheRegistry()
    {
        var registry = new FakeRegistry();
        var source = new RegistryProgramSource(registry);

        source.Collect(CancellationToken.None);

        Assert.Equal(UninstallLocation.All, registry.Requested);
        Assert.Equal(4, UninstallLocation.All.Count);
        Assert.Equal(ProgramSource.Registry, source.Source);
    }

    [Fact]
    public void Collect_Cancelled_Throws()
    {
        var source = new RegistryProgramSource(new FakeRegistry());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => source.Collect(cts.Token));
    }

    private sealed class FakeRegistry : IUninstallRegistry
    {
        public List<UninstallLocation> Requested { get; } = [];

        public IReadOnlyList<UninstallEntry> ReadEntries(UninstallLocation location, CancellationToken cancellationToken)
        {
            Requested.Add(location);
            return [];
        }
    }
}

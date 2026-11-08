using LeftoverScout.Core.Evidence;

namespace LeftoverScout.Core.Tests.Evidence;

public class StoreProgramSourceTests
{
    private sealed class FakePackage : IStorePackage
    {
        public Func<string> NameGetter { get; init; } = () => "Sample.App";

        public Func<string> FamilyNameGetter { get; init; } = () => "Sample.App_abc123";

        public Func<string> DisplayNameGetter { get; init; } = () => "Sample App";

        public Func<string> PublisherGetter { get; init; } = () => "Sample Publisher";

        public Func<string> VersionGetter { get; init; } = () => "1.2.3.4";

        public Func<string> LocationGetter { get; init; } = () => @"C:\Program Files\WindowsApps\Sample.App";

        public Func<bool> FrameworkGetter { get; init; } = () => false;

        public Func<bool> SystemGetter { get; init; } = () => false;

        public string Name => NameGetter();

        public string FamilyName => FamilyNameGetter();

        public string DisplayName => DisplayNameGetter();

        public string PublisherDisplayName => PublisherGetter();

        public string Version => VersionGetter();

        public string InstallLocation => LocationGetter();

        public bool IsFramework => FrameworkGetter();

        public bool IsSystem => SystemGetter();
    }

    private sealed class FakeProvider(params IStorePackage[] packages) : IStorePackageProvider
    {
        public IReadOnlyList<IStorePackage> GetPackages(CancellationToken cancellationToken) => packages;
    }

    private static IReadOnlyList<InstalledProgram> Collect(params IStorePackage[] packages) =>
        new StoreProgramSource(new FakeProvider(packages)).Collect(CancellationToken.None);

    [Fact]
    public void Collect_NormalPackage_MapsAllProperties()
    {
        InstalledProgram program = Assert.Single(Collect(new FakePackage()));

        Assert.Equal("Sample App", program.Name);
        Assert.Equal(ProgramSource.Store, program.Source);
        Assert.Equal("Sample.App_abc123", program.SourceDetail);
        Assert.Equal("Sample Publisher", program.Publisher);
        Assert.Equal("1.2.3.4", program.Version);
        Assert.Equal(@"C:\Program Files\WindowsApps\Sample.App", program.InstallLocation);
        Assert.False(program.IsSystemComponent);
        Assert.False(program.IsFramework);
        Assert.Null(program.InstallDate);
    }

    [Fact]
    public void Collect_SystemAndFrameworkFlags_AreMapped()
    {
        InstalledProgram program = Assert.Single(Collect(new FakePackage { SystemGetter = () => true, FrameworkGetter = () => true }));

        Assert.True(program.IsSystemComponent);
        Assert.True(program.IsFramework);
    }

    [Fact]
    public void Collect_EmptyDisplayName_FallsBackToIdentityName()
    {
        InstalledProgram program = Assert.Single(Collect(new FakePackage { DisplayNameGetter = () => string.Empty }));

        Assert.Equal("Sample.App", program.Name);
    }

    [Fact]
    public void Collect_PropertiesThrow_KeepsTheRest()
    {
        static string Throw() => throw new InvalidOperationException("sample failure");
        static bool ThrowBool() => throw new InvalidOperationException("sample failure");

        InstalledProgram program = Assert.Single(Collect(new FakePackage
        {
            DisplayNameGetter = Throw,
            PublisherGetter = Throw,
            VersionGetter = Throw,
            LocationGetter = Throw,
            FrameworkGetter = ThrowBool,
            SystemGetter = ThrowBool,
        }));

        Assert.Equal("Sample.App", program.Name);
        Assert.Equal("Sample.App_abc123", program.SourceDetail);
        Assert.Null(program.Publisher);
        Assert.Null(program.Version);
        Assert.Null(program.InstallLocation);
        Assert.False(program.IsSystemComponent);
        Assert.False(program.IsFramework);
    }

    [Fact]
    public void Collect_NoNameAtAll_SkipsPackageButKeepsOthers()
    {
        static string Throw() => throw new InvalidOperationException("sample failure");

        IReadOnlyList<InstalledProgram> programs = Collect(
            new FakePackage { NameGetter = Throw, DisplayNameGetter = Throw },
            new FakePackage { DisplayNameGetter = () => "Other App" });

        Assert.Equal(["Other App"], programs.Select(p => p.Name));
    }

    [Fact]
    public void Collect_FamilyNameThrows_UsesIdentityNameAsDetail()
    {
        static string Throw() => throw new InvalidOperationException("sample failure");

        InstalledProgram program = Assert.Single(Collect(new FakePackage { FamilyNameGetter = Throw }));

        Assert.Equal("Sample.App", program.SourceDetail);
    }

    [Fact]
    public void Collect_Cancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => new StoreProgramSource(new FakeProvider(new FakePackage())).Collect(cts.Token));
    }
}

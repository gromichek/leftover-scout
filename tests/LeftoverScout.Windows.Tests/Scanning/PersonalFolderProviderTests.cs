using LeftoverScout.Windows.Scanning;

namespace LeftoverScout.Windows.Tests.Scanning;

public class PersonalFolderProviderTests
{
    [Fact]
    public void GetPersonalFolderPaths_AddsOneDriveRootsFromEnvironment()
    {
        var variables = new Dictionary<string, string?>
        {
            ["OneDrive"] = @"C:\Users\SampleUser\OneDrive",
            ["OneDriveCommercial"] = @"C:\Users\SampleUser\OneDrive - Sample Company",
            ["OneDriveConsumer"] = "  ",
        };
        var provider = new PersonalFolderProvider(name => variables.GetValueOrDefault(name), () => [@"C:\Users\SampleUser\Documents"]);

        IReadOnlyList<string> paths = provider.GetPersonalFolderPaths();

        Assert.Equal(
            new[] { @"C:\Users\SampleUser\Documents", @"C:\Users\SampleUser\OneDrive", @"C:\Users\SampleUser\OneDrive - Sample Company" },
            paths);
    }

    [Fact]
    public void GetPersonalFolderPaths_RealKnownFolders_AreFullyQualified()
    {
        var provider = new PersonalFolderProvider(_ => null, () => []);
        Assert.Empty(provider.GetPersonalFolderPaths());

        // Reads only the known folder locations from the system; nothing on disk is touched.
        IReadOnlyList<string> real = new PersonalFolderProvider().GetPersonalFolderPaths();

        Assert.All(real, path => Assert.True(Path.IsPathFullyQualified(path)));
    }
}

using LeftoverScout.Core.Classification;

namespace LeftoverScout.Core.Tests.Classification;

public class PackageNamesTests
{
    [Theory]
    [InlineData("Kaspersky.ShellEx22_b81m8cbssw9gt", "Kaspersky")]
    [InlineData("Contoso_abc123", "Contoso")]
    [InlineData("Contoso.Sub.App_abc123", "Contoso")]
    [InlineData("Contoso.App", "Contoso")]
    [InlineData("Contoso", "Contoso")]
    public void VendorPart_TakesTextBeforeFirstDotOfThePackageName(string folder, string expected)
    {
        Assert.Equal(expected, PackageNames.VendorPart(folder));
    }

    [Theory]
    [InlineData("Kaspersky.ShellEx22_b81m8cbssw9gt", "Kaspersky.ShellEx22")]
    [InlineData("A_B_C", "A_B")]
    [InlineData("NoUnderscore", "NoUnderscore")]
    public void PackageName_CutsAtTheLastUnderscore(string folder, string expected)
    {
        Assert.Equal(expected, PackageNames.PackageName(folder));
    }
}

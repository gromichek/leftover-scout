using LeftoverScout.Windows.Evidence;

namespace LeftoverScout.Windows.Tests.Evidence;

public class StorePackageMergerTests
{
    [Fact]
    public void Merge_DuplicateFullNames_AreKeptOnce_CaseInsensitive()
    {
        string[] main = ["Contoso.A_1.0_x64__abc", "Contoso.B_1.0_x64__abc"];
        string[] framework = ["contoso.a_1.0_x64__abc", "Contoso.Fw_1.0_x64__abc"];

        List<string> merged = StorePackageMerger.Merge<string>([main, framework], name => name);

        Assert.Equal(new[] { "Contoso.A_1.0_x64__abc", "Contoso.B_1.0_x64__abc", "Contoso.Fw_1.0_x64__abc" }, merged);
    }

    [Fact]
    public void Merge_PackagesWithoutFullName_AreKept()
    {
        List<string?> merged = StorePackageMerger.Merge<string?>([new string?[] { null, "" }], name => name);

        Assert.Equal(2, merged.Count);
    }

    [Fact]
    public void Merge_NoResults_GivesEmptyList()
    {
        Assert.Empty(StorePackageMerger.Merge<string>([], name => name));
    }
}

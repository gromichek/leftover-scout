using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Evidence;
using static LeftoverScout.Core.Tests.Classification.Evidences;

namespace LeftoverScout.Core.Tests.Classification;

public class EvidenceIndexTests
{
    private static EvidenceMatch? Match(string folderName, params InstalledProgram[] programs) =>
        new EvidenceIndex(Snapshot(programs)).Match(NameNormalizer.Normalize(folderName));

    [Fact]
    public void Match_EqualNormalizedNames_Matches()
    {
        EvidenceMatch? match = Match("foo-soft", Registry("FooSoft 2022"));

        Assert.Equal("FooSoft 2022", match?.ProgramName);
        Assert.False(match!.OnlyPublisher);
    }

    [Fact]
    public void Match_AllTokensOfFolderInOneEntry_Matches()
    {
        Assert.NotNull(Match("Studio Code", Registry("Visual Studio Code")));
    }

    [Fact]
    public void Match_AllTokensOfEntryInFolder_Matches()
    {
        Assert.NotNull(Match("FooSoft Backup", Registry("FooSoft")));
    }

    [Fact]
    public void Match_PublisherOnly_IsMarkedAsPublisherMatch()
    {
        EvidenceMatch? match = Match("JetBrains", Registry("Some Tool", publisher: "JetBrains s.r.o."));

        Assert.NotNull(match);
        Assert.True(match.OnlyPublisher);
    }

    [Fact]
    public void Match_NameAndPublisher_IsNotPublisherOnly()
    {
        EvidenceMatch? match = Match("Contoso", Registry("Contoso", publisher: "Contoso Ltd"), Registry("Other", publisher: "Contoso Ltd"));

        Assert.False(match!.OnlyPublisher);
    }

    [Theory]
    [InlineData("Foo", "Food")]
    [InlineData("Food", "Foo")]
    [InlineData("FooSof", "FooSoft")]
    [InlineData("Fooo", "Foo")]
    public void Match_SimilarButDifferentNames_DoNotMatch(string folderName, string program)
    {
        Assert.Null(Match(folderName, Registry(program)));
    }

    [Fact]
    public void Match_TokensShorterThanThreeCharacters_AreIgnoredInContainsChecks()
    {
        Assert.Null(Match("AB Cleaner", Registry("AB Tools")));
        Assert.Null(Match("AB", Registry("AB Tools")));
        Assert.NotNull(Match("AB Tools", Registry("AB Tools")));
    }

    [Fact]
    public void Match_GenericTokensOfAnEntry_DoNotDecide()
    {
        // "Microsoft Update" has no significant token, so it must not match every folder that mentions "microsoft".
        Assert.Null(Match("Microsoft Foo", Registry("Microsoft Update")));
    }

    [Fact]
    public void Match_RegistryKeyName_IsIndexed()
    {
        Assert.NotNull(Match("BarKey", Registry("Display Name", key: "BarKey")));
    }

    [Fact]
    public void Match_StoreIdentityNameFromFamily_IsIndexed()
    {
        EvidenceMatch? match = Match("Contoso.Notes", Store("Notes", "Contoso.Notes_abc123"));

        Assert.Equal("Notes", match?.ProgramName);
    }

    [Fact]
    public void Match_ProgramFolderAndItsSubfolders_AreIndexed()
    {
        InstalledProgram folder = ProgramFolder("Vendor Suite", "Alpha Editor");

        Assert.NotNull(Match("Vendor Suite", folder));
        Assert.NotNull(Match("AlphaEditor", folder));
    }

    [Fact]
    public void Match_StartMenuEntry_IsIndexed()
    {
        Assert.NotNull(Match("Quick Notes", StartMenu("Quick Notes")));
    }

    [Fact]
    public void Match_PrefersAProgramNameOverAPublisher()
    {
        EvidenceMatch? match = Match("Contoso", Registry("Zeta", publisher: "Contoso"), Registry("Contoso"));

        Assert.Equal("Contoso", match?.ProgramName);
    }

    [Fact]
    public void FindStoreFamily_IsCaseInsensitive()
    {
        var index = new EvidenceIndex(Snapshot(Store("Notes", "Contoso.Notes_abc123")));

        Assert.Equal("Notes", index.FindStoreFamily("contoso.NOTES_ABC123"));
        Assert.Null(index.FindStoreFamily("Contoso.Notes_other"));
    }

    [Fact]
    public void MatchesHint_ComparesNamesButNotPublishers()
    {
        var index = new EvidenceIndex(Snapshot(Registry("Other Tool", publisher: "Contoso Ltd")));

        Assert.True(index.MatchesHint("Other Tool"));
        Assert.False(index.MatchesHint("Contoso"));
    }

    [Fact]
    public void Match_EmptyName_DoesNotMatch()
    {
        Assert.Null(new EvidenceIndex(Snapshot(Registry("Sample"))).Match(NormalizedName.Empty));
    }
}

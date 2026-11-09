using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Results;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Classification;

namespace LeftoverScout.Core.Tests.Results;

public class LeftoverLabelTests
{
    [Theory]
    [InlineData(LeftoverConfidence.High, "Leftover: High")]
    [InlineData(LeftoverConfidence.Medium, "Leftover: Medium")]
    [InlineData(LeftoverConfidence.Low, "Leftover: Low")]
    public void Describe_LeftoverVerdict_ShowsLeftoverLabel(LeftoverConfidence confidence, string expected)
    {
        LeftoverVerdict verdict = LeftoverVerdict.Leftover(confidence, "Because.");

        Assert.Equal(expected, FolderTagText.Describe(FolderTags.None, null, verdict));
    }

    [Fact]
    public void Describe_InUseVerdict_ShowsProgram()
    {
        LeftoverVerdict verdict = LeftoverVerdict.InUse("Discord", "Belongs to Discord (installed).");

        Assert.Equal("In use: Discord", FolderTagText.Describe(FolderTags.None, null, verdict));
    }

    [Fact]
    public void Describe_UnknownVerdict_ShowsNoLabel()
    {
        LeftoverVerdict verdict = LeftoverVerdict.Unknown("Generic name.");

        Assert.Equal(string.Empty, FolderTagText.Describe(FolderTags.None, null, verdict));
        Assert.Equal(string.Empty, FolderTagText.Explain(FolderTags.None, null, verdict));
    }

    [Fact]
    public void Describe_NoVerdict_KeepsTheExistingText()
    {
        Assert.Equal("Link", FolderTagText.Describe(FolderTags.Link, null, null));
        Assert.Equal("Link", FolderTagText.Describe(FolderTags.Link, null));
    }

    [Fact]
    public void Describe_TagAndVerdict_AreJoinedWithComma()
    {
        LeftoverVerdict verdict = LeftoverVerdict.Leftover(LeftoverConfidence.Low, "Because.");

        Assert.Equal("Inaccessible, Leftover: Low", FolderTagText.Describe(FolderTags.Inaccessible, null, verdict));
    }

    [Fact]
    public void Explain_Verdict_AddsItsExplanationAfterTheTagExplanation()
    {
        LeftoverVerdict verdict = LeftoverVerdict.Leftover(LeftoverConfidence.Medium, "No installed program named 'FooSoft' was found.");

        string[] lines = FolderTagText.Explain(FolderTags.Inaccessible, null, verdict).Split('\n');

        Assert.StartsWith("Inaccessible:", lines[0], StringComparison.Ordinal);
        Assert.Equal("No installed program named 'FooSoft' was found.", lines[^1]);
    }

    [Fact]
    public void Build_WithLeftovers_PutsTheVerdictOnTheRow()
    {
        FolderNode root = new TreeBuilder().File("Alpha", "a.dat", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Build();
        FolderNode alpha = root.Children.Single();
        var verdicts = new Dictionary<FolderNode, LeftoverVerdict> { [alpha] = LeftoverVerdict.Unknown("x") };
        LeftoverMap map = new(verdicts);

        FolderRow row = FolderRowBuilder.Build(root, null, map).Single(item => !item.IsFilesRow);

        Assert.Same(map.Get(alpha), row.Leftover);
        Assert.Null(FolderRowBuilder.Build(root).Single(item => !item.IsFilesRow).Leftover);
    }
}

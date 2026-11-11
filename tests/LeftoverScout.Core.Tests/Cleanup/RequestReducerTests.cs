using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;

namespace LeftoverScout.Core.Tests.Cleanup;

public class RequestReducerTests
{
    private const string P = @"C:\Users\SampleUser\AppData\Roaming\";

    private static CleanupItem Item(string path) => new(path, FindingKind.Cache, null, 100, null);

    private static string[] Paths(RequestReduction reduction) => [.. reduction.Items.Select(item => item.FullPath)];

    [Fact]
    public void Reduce_NoItems_ReturnsNothing()
    {
        RequestReduction reduction = RequestReducer.Reduce([]);

        Assert.Empty(reduction.Items);
        Assert.Empty(reduction.Merged);
    }

    [Fact]
    public void Reduce_UnrelatedItems_AreAllKeptInOrder()
    {
        RequestReduction reduction = RequestReducer.Reduce([Item(P + "B"), Item(P + "A"), Item(P + "C")]);

        Assert.Equal([P + "B", P + "A", P + "C"], Paths(reduction));
        Assert.Empty(reduction.Merged);
    }

    [Fact]
    public void Reduce_Duplicates_AreRemovedIgnoringCaseSlashesAndTrailingSeparator()
    {
        RequestReduction reduction = RequestReducer.Reduce(
            [Item(P + "App"), Item(P.ToUpperInvariant() + "APP"), Item(P + "App\\"), Item(P.Replace('\\', '/') + "App")]);

        Assert.Equal([P + "App"], Paths(reduction));
        Assert.Equal(3, reduction.Merged.Count);
        Assert.All(reduction.Merged, merged =>
        {
            Assert.Equal(MergeReason.Duplicate, merged.Reason);
            Assert.Equal(P + "App", merged.CoveredBy);
        });
    }

    [Fact]
    public void Reduce_ItemInsideAnotherSelectedItem_IsDropped()
    {
        RequestReduction reduction = RequestReducer.Reduce([Item(P + @"App\Cache\Deep"), Item(P + "App"), Item(P + "Other")]);

        Assert.Equal([P + "App", P + "Other"], Paths(reduction));
        MergedItem merged = Assert.Single(reduction.Merged);
        Assert.Equal(P + @"App\Cache\Deep", merged.Item.FullPath);
        Assert.Equal(P + "App", merged.CoveredBy);
        Assert.Equal(MergeReason.InsideSelectedFolder, merged.Reason);
    }

    [Fact]
    public void Reduce_ChainOfNestedItems_KeepsOnlyTheOutermost()
    {
        RequestReduction reduction = RequestReducer.Reduce([Item(P + @"A\B\C"), Item(P + @"A\B"), Item(P + "A")]);

        Assert.Equal([P + "A"], Paths(reduction));
        Assert.Equal(2, reduction.Merged.Count);
        Assert.All(reduction.Merged, merged => Assert.Equal(P + "A", merged.CoveredBy));
    }

    [Fact]
    public void Reduce_SiblingWithSamePrefix_IsNotNested()
    {
        RequestReduction reduction = RequestReducer.Reduce([Item(P + "App"), Item(P + "App2"), Item(P + "App.old")]);

        Assert.Equal([P + "App", P + "App2", P + "App.old"], Paths(reduction));
        Assert.Empty(reduction.Merged);
    }

    [Fact]
    public void Reduce_UnparsablePaths_AreKeptSoThatTheGuardBlocksThem()
    {
        RequestReduction reduction = RequestReducer.Reduce([Item(@"relative\path"), Item(@"relative\path"), Item(P + "App")]);

        Assert.Equal([@"relative\path", P + "App"], Paths(reduction));
        Assert.Single(reduction.Merged);
    }
}

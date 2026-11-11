using LeftoverScout.Core.Classification;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Core.Findings;
using LeftoverScout.Core.Scanning;
using LeftoverScout.Core.Tests.Scanning;

namespace LeftoverScout.Core.Tests.Cleanup;

public class RunningProgramDetectorTests
{
    private const string Root = FakeDirectoryReader.Root;

    private sealed class FakeProcesses(params ProcessInfo[] processes) : IProcessLister
    {
        public IReadOnlyList<ProcessInfo> GetProcesses() => processes;
    }

    private static IReadOnlyList<RunningProgramWarning> Find(CleanupItem item, params ProcessInfo[] processes) =>
        new RunningProgramDetector(new FakeProcesses(processes))
            .Find(new CleanupRequest(new ScanTarget(Root, isCurrentUser: false), [item]));

    private static CleanupItem Item(string folder, string? program = null) =>
        new(Root + @"\AppData\Roaming\" + folder, FindingKind.Leftover, LeftoverConfidence.High, 1, null, program);

    [Fact]
    public void Find_ProcessImageInsideTheItem_Matches()
    {
        var item = Item("FictionalTool");

        var warning = Assert.Single(Find(item, new ProcessInfo("helper", Root + @"\AppData\Roaming\FictionalTool\bin\helper.exe")));

        Assert.Equal(["helper"], warning.ProcessNames);
    }

    [Fact]
    public void Find_ProcessNameMatchesFolderName_Matches()
    {
        var warning = Assert.Single(Find(Item("Fictional Tool"), new ProcessInfo("FictionalTool", null)));

        Assert.Equal(["FictionalTool"], warning.ProcessNames);
    }

    [Fact]
    public void Find_ProcessNameMatchesProgramName_Matches()
    {
        var warning = Assert.Single(Find(Item("ft-data", "Fictional Tool"), new ProcessInfo("fictionaltool.exe", null)));

        Assert.Single(warning.ProcessNames);
    }

    [Fact]
    public void Find_UnrelatedProcess_NoWarning()
    {
        Assert.Empty(Find(Item("FictionalTool"), new ProcessInfo("notepad", @"C:\Windows\notepad.exe")));
    }

    [Fact]
    public void Find_SimilarFolderNamePrefix_DoesNotMatchByPath()
    {
        Assert.Empty(Find(Item("Tool"), new ProcessInfo("other", Root + @"\AppData\Roaming\Tool2\x.exe")));
    }

    [Fact]
    public void Find_GenericFolderName_DoesNotMatchByName()
    {
        Assert.Empty(Find(Item("Cache"), new ProcessInfo("cache", null)));
    }

    [Fact]
    public void Find_SeveralProcesses_ListedOnceSorted()
    {
        var item = Item("FictionalTool");
        string image = Root + @"\AppData\Roaming\FictionalTool\a.exe";

        var warning = Assert.Single(Find(item, new ProcessInfo("zeta", image), new ProcessInfo("alpha", image), new ProcessInfo("alpha", image)));

        Assert.Equal(["alpha", "zeta"], warning.ProcessNames);
    }
}

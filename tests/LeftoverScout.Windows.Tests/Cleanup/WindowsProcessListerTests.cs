using LeftoverScout.Core.Cleanup;
using LeftoverScout.Windows.Cleanup;

namespace LeftoverScout.Windows.Tests.Cleanup;

public class WindowsProcessListerTests
{
    [Fact]
    public void GetProcesses_IncludesTheTestProcessWithItsImagePath()
    {
        IReadOnlyList<ProcessInfo> processes = new WindowsProcessLister().GetProcesses();

        string own = Environment.ProcessPath!;

        Assert.Contains(processes, process => string.Equals(process.ImagePath, own, StringComparison.OrdinalIgnoreCase));
    }
}

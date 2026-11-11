namespace LeftoverScout.Core.Cleanup;

/// <summary>Lists the running processes. Implementations only read; they never touch a process.</summary>
public interface IProcessLister
{
    /// <summary>Returns the running processes. Never throws.</summary>
    IReadOnlyList<ProcessInfo> GetProcesses();
}

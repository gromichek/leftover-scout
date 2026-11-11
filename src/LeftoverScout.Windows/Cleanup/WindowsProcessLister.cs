using System.Diagnostics;
using LeftoverScout.Core.Cleanup;
using LeftoverScout.Windows.Interop;

namespace LeftoverScout.Windows.Cleanup;

/// <summary>
/// Lists the running processes for the running-program warning (D-064): names through <c>Process.GetProcesses()</c>,
/// image paths through <c>QueryFullProcessImageNameW</c> with <c>PROCESS_QUERY_LIMITED_INFORMATION</c>. Failures
/// (access denied, process gone) are ignored. No process is touched.
/// </summary>
public sealed class WindowsProcessLister : IProcessLister
{
    private const uint ProcessQueryLimitedInformation = 0x1000;

    /// <inheritdoc />
    public IReadOnlyList<ProcessInfo> GetProcesses()
    {
        var result = new List<ProcessInfo>();
        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (InvalidOperationException)
        {
            return result;
        }

        foreach (Process process in processes)
        {
            try
            {
                result.Add(new ProcessInfo(process.ProcessName, QueryImagePath(process.Id)));
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // The process ended or cannot be read: ignore it.
            }
            finally
            {
                process.Dispose();
            }
        }

        return result;
    }

    private static unsafe string? QueryImagePath(int processId)
    {
        nint handle = NativeMethods.OpenProcess(ProcessQueryLimitedInformation, false, (uint)processId);
        if (handle == 0)
        {
            return null;
        }

        try
        {
            char[] buffer = new char[32768];
            uint size = (uint)buffer.Length;
            fixed (char* start = buffer)
            {
                return NativeMethods.QueryFullProcessImageName(handle, 0, start, ref size) ? new string(start, 0, (int)size) : null;
            }
        }
        finally
        {
            NativeMethods.CloseHandle(handle);
        }
    }
}

using System.Diagnostics;

namespace LeftoverScout.Windows.Tests.Cleanup;

/// <summary>Creates directory junctions in test folders (mklink /J does not need administrator rights).</summary>
internal static class JunctionHelper
{
    public static bool TryCreate(string link, string target)
    {
        var startInfo = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("mklink");
        startInfo.ArgumentList.Add("/J");
        startInfo.ArgumentList.Add(link);
        startInfo.ArgumentList.Add(target);

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 && Directory.Exists(link);
    }

    /// <summary>Removes only the junction itself (non-recursive), never the content of its target.</summary>
    public static void Remove(string link) => Directory.Delete(link);
}

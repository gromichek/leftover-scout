using System.ComponentModel;
using System.Diagnostics;
using LeftoverScout.Core.Shell;

namespace LeftoverScout.Windows.Shell;

/// <summary>
/// Opens folders in Explorer and links in the default browser through the Windows shell.
/// </summary>
public sealed class ShellLauncher : IShellLauncher
{
    /// <inheritdoc />
    public void OpenFolder(string folderPath)
    {
        // Only open an existing folder; ShellExecute on a folder path opens Explorer.
        if (Directory.Exists(folderPath))
        {
            Start(folderPath);
        }
    }

    /// <inheritdoc />
    public void OpenUrl(string url)
    {
        // Only web addresses are opened, never arbitrary files or programs.
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            Start(uri.AbsoluteUri);
        }
    }

    private static void Start(string target)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            // Nothing sensible to do if the shell cannot open it; the app keeps working.
        }
    }
}

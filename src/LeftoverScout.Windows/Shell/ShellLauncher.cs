using System.ComponentModel;
using System.Diagnostics;
using LeftoverScout.Core.Shell;
using Microsoft.Extensions.Logging;

namespace LeftoverScout.Windows.Shell;

/// <summary>
/// Opens folders in Explorer and links in the default browser through the Windows shell.
/// </summary>
public sealed class ShellLauncher : IShellLauncher
{
    private readonly ILogger<ShellLauncher> _logger;

    /// <summary>Creates the launcher.</summary>
    public ShellLauncher(ILogger<ShellLauncher> logger)
    {
        _logger = logger;
    }

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
    public void SelectFile(string filePath)
    {
        string? arguments = BuildSelectArguments(filePath);
        if (arguments is null || !File.Exists(filePath))
        {
            _logger.LogWarning("Cannot show the file in Explorer: the path is invalid or the file no longer exists.");
            return;
        }

        try
        {
            // explorer.exe /select,"<path>" opens the folder with the file selected.
            // The path is quoted as a whole; Windows file names cannot contain a double quote.
            using Process? process = Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = false });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            _logger.LogWarning(ex, "Explorer could not be started to show a file.");
        }
    }

    /// <inheritdoc />
    public void OpenRecycleBin()
    {
        try
        {
            // "shell:RecycleBinFolder" is the documented shell: folder name of the Recycle Bin.
            using Process? process = Process.Start(new ProcessStartInfo("explorer.exe", RecycleBinArguments) { UseShellExecute = false });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            _logger.LogWarning(ex, "Explorer could not be started to show the Recycle Bin.");
        }
    }

    /// <summary>The argument string that makes explorer.exe show the Recycle Bin.</summary>
    internal const string RecycleBinArguments = "shell:RecycleBinFolder";

    /// <summary>
    /// Builds the argument string <c>/select,"path"</c> for explorer.exe; null for an empty path or one with a double quote.
    /// </summary>
    internal static string? BuildSelectArguments(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath.Contains('"'))
        {
            return null;
        }

        return $"/select,\"{filePath}\"";
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

    private void Start(string target)
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            // Nothing sensible to do if the shell cannot open it; the app keeps working.
            _logger.LogWarning(ex, "The shell could not open the requested target.");
        }
    }
}

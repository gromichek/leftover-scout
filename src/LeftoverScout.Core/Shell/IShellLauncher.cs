namespace LeftoverScout.Core.Shell;

/// <summary>
/// Opens a folder in the file manager or a link in the default browser.
/// </summary>
public interface IShellLauncher
{
    /// <summary>Opens the folder in Windows Explorer. Never throws.</summary>
    void OpenFolder(string folderPath);

    /// <summary>Opens the address in the default browser. Never throws.</summary>
    void OpenUrl(string url);
}

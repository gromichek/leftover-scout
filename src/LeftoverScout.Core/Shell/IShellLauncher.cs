namespace LeftoverScout.Core.Shell;

/// <summary>
/// Opens a folder or file location in the file manager, or a link in the default browser.
/// </summary>
public interface IShellLauncher
{
    /// <summary>Opens the folder in Windows Explorer. Never throws.</summary>
    void OpenFolder(string folderPath);

    /// <summary>Opens Windows Explorer with the file selected in its folder. Never throws.</summary>
    void SelectFile(string filePath);

    /// <summary>Opens the Recycle Bin in Windows Explorer (<c>explorer.exe shell:RecycleBinFolder</c>). Never throws.</summary>
    void OpenRecycleBin();

    /// <summary>Opens the address in the default browser. Never throws.</summary>
    void OpenUrl(string url);
}

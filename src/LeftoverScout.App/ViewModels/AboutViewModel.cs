using CommunityToolkit.Mvvm.Input;
using LeftoverScout.Core.Shell;
using LeftoverScout.Core.Storage;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// The About page: name, version, license, repository link and the data folder.
/// </summary>
public sealed partial class AboutViewModel : PageViewModel
{
    /// <summary>Address of the public repository.</summary>
    public const string RepositoryUrl = "https://github.com/gromichek/leftover-scout";

    private readonly IShellLauncher _shell;

    /// <summary>Creates the About page.</summary>
    /// <param name="version">Version text to display.</param>
    /// <param name="dataLocation">Where settings and logs are stored.</param>
    /// <param name="shell">Used to open Explorer and the browser.</param>
    public AboutViewModel(string version, AppDataLocation dataLocation, IShellLauncher shell)
        : base("About")
    {
        Version = version;
        DataFolderPath = dataLocation.Path;
        _shell = shell;
    }

    /// <summary>Application name.</summary>
    public string AppName => "LeftoverScout";

    /// <summary>Version text, for example "0.1.0".</summary>
    public string Version { get; }

    /// <summary>License name.</summary>
    public string License => "MIT License";

    /// <summary>Repository address.</summary>
    public string Repository => RepositoryUrl;

    /// <summary>Full path of the data folder.</summary>
    public string DataFolderPath { get; }

    [RelayCommand]
    private void OpenDataFolder() => _shell.OpenFolder(DataFolderPath);

    [RelayCommand]
    private void OpenRepository() => _shell.OpenUrl(RepositoryUrl);
}

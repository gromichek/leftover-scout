namespace LeftoverScout.Core.Storage;

/// <summary>
/// Decides where the application stores its data (decision D-012): a <c>data</c> folder next to
/// the program if it is writable, otherwise <c>%LOCALAPPDATA%\LeftoverScout</c>.
/// </summary>
public sealed class AppDataLocationResolver
{
    /// <summary>Name of the portable data folder next to the program.</summary>
    public const string PortableFolderName = "data";

    /// <summary>Name of the fallback folder inside the local application data folder.</summary>
    public const string FallbackFolderName = "LeftoverScout";

    private readonly IDataFolderProbe _probe;

    /// <summary>Creates a resolver that uses <paramref name="probe"/> to test candidate folders.</summary>
    public AppDataLocationResolver(IDataFolderProbe probe)
    {
        _probe = probe;
    }

    /// <summary>
    /// Chooses the data folder.
    /// </summary>
    /// <param name="programFolder">Folder that contains the executable.</param>
    /// <param name="localAppDataFolder">The user's local application data folder.</param>
    public AppDataLocation Resolve(string programFolder, string localAppDataFolder)
    {
        string portable = Path.Combine(programFolder, PortableFolderName);
        if (_probe.TryEnsureWritable(portable))
        {
            return new AppDataLocation(portable, IsFallback: false);
        }

        string fallback = Path.Combine(localAppDataFolder, FallbackFolderName);

        // The result of this call is intentionally ignored: even if the fallback cannot be prepared,
        // the app must still start. The settings store and the logger tolerate write errors.
        _ = _probe.TryEnsureWritable(fallback);
        return new AppDataLocation(fallback, IsFallback: true);
    }
}

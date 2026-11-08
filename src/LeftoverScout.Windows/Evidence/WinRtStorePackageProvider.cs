using LeftoverScout.Core.Evidence;
using Windows.Management.Deployment;

namespace LeftoverScout.Windows.Evidence;

/// <summary>
/// Lists the Store / MSIX packages of the current user with
/// <c>PackageManager.FindPackagesForUser(string.Empty)</c> (an empty user SID means the current user; no
/// administrator rights needed). Docs: "PackageManager.FindPackagesForUser", Microsoft Learn (decision D-036).
/// </summary>
public sealed class WinRtStorePackageProvider : IStorePackageProvider
{
    /// <inheritdoc />
    public IReadOnlyList<IStorePackage> GetPackages(CancellationToken cancellationToken)
    {
        var manager = new PackageManager();
        var packages = new List<IStorePackage>();
        foreach (var package in manager.FindPackagesForUser(string.Empty))
        {
            cancellationToken.ThrowIfCancellationRequested();
            packages.Add(new WinRtStorePackage(package));
        }

        return packages;
    }
}

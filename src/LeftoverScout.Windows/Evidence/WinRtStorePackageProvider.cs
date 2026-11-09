using LeftoverScout.Core.Evidence;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace LeftoverScout.Windows.Evidence;

/// <summary>
/// Lists the Store / MSIX packages of the current user with
/// <c>PackageManager.FindPackagesForUserWithPackageTypes(string.Empty, types)</c> (an empty user SID means the current
/// user; no administrator rights needed; decisions D-036, D-050). All package types except Xap (Windows Phone) are
/// requested and the result is de-duplicated by package full name.
/// </summary>
public sealed class WinRtStorePackageProvider : IStorePackageProvider
{
    // Main, Framework, Resource, Bundle and Optional; combined into one query.
    private const PackageTypes AllTypes =
        PackageTypes.Main | PackageTypes.Framework | PackageTypes.Resource | PackageTypes.Bundle | PackageTypes.Optional;

    /// <inheritdoc />
    public IReadOnlyList<IStorePackage> GetPackages(CancellationToken cancellationToken)
    {
        var manager = new PackageManager();
        var packages = new List<Package>();
        foreach (var package in manager.FindPackagesForUserWithPackageTypes(string.Empty, AllTypes))
        {
            cancellationToken.ThrowIfCancellationRequested();
            packages.Add(package);
        }

        return StorePackageMerger.Merge([packages], FullName)
            .Select(package => (IStorePackage)new WinRtStorePackage(package))
            .ToList();
    }

    private static string? FullName(Package package)
    {
        try
        {
            return package.Id.FullName;
        }
        catch (Exception)
        {
            return null; // Per-package error handling: a package whose id cannot be read is kept.
        }
    }
}

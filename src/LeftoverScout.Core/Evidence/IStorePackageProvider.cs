namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Lists the Store / MSIX packages of the current user. Read-only.
/// </summary>
public interface IStorePackageProvider
{
    /// <summary>Returns the packages installed for the current user.</summary>
    IReadOnlyList<IStorePackage> GetPackages(CancellationToken cancellationToken);
}

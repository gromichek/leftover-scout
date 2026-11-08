using LeftoverScout.Core.Evidence;
using Windows.ApplicationModel;

namespace LeftoverScout.Windows.Evidence;

/// <summary>
/// Adapts a WinRT <see cref="Package"/> to <see cref="IStorePackage"/>. Every property reads the package when
/// it is called, so a property that throws for one package only affects that property.
/// </summary>
internal sealed class WinRtStorePackage(Package package) : IStorePackage
{
    public string Name => package.Id.Name;

    public string FamilyName => package.Id.FamilyName;

    public string DisplayName => package.DisplayName;

    public string PublisherDisplayName => package.PublisherDisplayName;

    public string Version
    {
        get
        {
            PackageVersion version = package.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }

    public string InstallLocation => package.InstalledLocation.Path;

    public bool IsFramework => package.IsFramework;

    public bool IsSystem => package.SignatureKind == PackageSignatureKind.System;
}

namespace LeftoverScout.Core.Evidence;

/// <summary>
/// One Store / MSIX package. Any property may throw for some packages; <see cref="StoreProgramSource"/>
/// catches that per property.
/// </summary>
public interface IStorePackage
{
    /// <summary>Package identity name (<c>Package.Id.Name</c>).</summary>
    string Name { get; }

    /// <summary>Package family name (<c>Package.Id.FamilyName</c>).</summary>
    string FamilyName { get; }

    /// <summary>Display name (<c>Package.DisplayName</c>); may be empty.</summary>
    string DisplayName { get; }

    /// <summary>Publisher display name (<c>Package.PublisherDisplayName</c>).</summary>
    string PublisherDisplayName { get; }

    /// <summary>Version text (<c>Package.Id.Version</c>).</summary>
    string Version { get; }

    /// <summary>Install folder (<c>Package.InstalledLocation.Path</c>).</summary>
    string InstallLocation { get; }

    /// <summary>True for framework packages (<c>Package.IsFramework</c>).</summary>
    bool IsFramework { get; }

    /// <summary>True when the package is signed as part of the system (<c>Package.SignatureKind == System</c>).</summary>
    bool IsSystem { get; }
}

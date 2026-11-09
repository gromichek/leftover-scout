namespace LeftoverScout.Core.Classification;

/// <summary>Where a leftover candidate was found (decision D-044).</summary>
public enum CandidateKind
{
    /// <summary>A first-level folder of Roaming, Local or LocalLow.</summary>
    AppDataFolder,

    /// <summary>A second-level folder below a vendor folder.</summary>
    VendorChild,

    /// <summary>A folder that starts with a dot, directly in the profile root.</summary>
    DotFolder,

    /// <summary>A folder in <c>AppData\Local\Packages</c> (named after a Store package family).</summary>
    StorePackage,

    /// <summary>A child of the <c>Microsoft</c> folder; these are not evaluated yet and stay unknown.</summary>
    MicrosoftChild,
}

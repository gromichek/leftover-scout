namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Read-only access to the Uninstall registry keys. Implementations must open keys read-only and never write.
/// </summary>
public interface IUninstallRegistry
{
    /// <summary>
    /// Reads all subkeys of the Uninstall key at <paramref name="location"/>. A missing key gives an empty list;
    /// a subkey that cannot be read is skipped.
    /// </summary>
    IReadOnlyList<UninstallEntry> ReadEntries(UninstallLocation location, CancellationToken cancellationToken);
}

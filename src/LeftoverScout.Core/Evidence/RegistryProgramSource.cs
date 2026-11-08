namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Evidence from the Uninstall registry keys (decision D-035, source 1). Read-only.
/// </summary>
public sealed class RegistryProgramSource : IInstalledProgramSource
{
    private readonly IUninstallRegistry _registry;

    /// <summary>Creates the source.</summary>
    public RegistryProgramSource(IUninstallRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <inheritdoc />
    public ProgramSource Source => ProgramSource.Registry;

    /// <inheritdoc />
    public IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken)
    {
        var read = new List<(UninstallLocation, IReadOnlyList<UninstallEntry>)>();
        foreach (UninstallLocation location in UninstallLocation.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            read.Add((location, _registry.ReadEntries(location, cancellationToken)));
        }

        return UninstallEntryParser.ParseAll(read);
    }
}

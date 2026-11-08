namespace LeftoverScout.Core.Evidence;

/// <summary>
/// Evidence from the Store / MSIX packages of the current user (decision D-035, source 2; D-036).
/// A property that throws for one package is skipped; the other properties are kept.
/// </summary>
public sealed class StoreProgramSource : IInstalledProgramSource
{
    private readonly IStorePackageProvider _provider;

    /// <summary>Creates the source.</summary>
    public StoreProgramSource(IStorePackageProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <inheritdoc />
    public ProgramSource Source => ProgramSource.Store;

    /// <inheritdoc />
    public IReadOnlyList<InstalledProgram> Collect(CancellationToken cancellationToken)
    {
        var programs = new List<InstalledProgram>();
        foreach (IStorePackage package in _provider.GetPackages(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? identityName = Read(() => package.Name);
            string? name = Read(() => package.DisplayName) ?? identityName;
            if (name is null)
            {
                continue;
            }

            string? familyName = Read(() => package.FamilyName);
            programs.Add(new InstalledProgram(
                name,
                ProgramSource.Store,
                familyName ?? identityName ?? name,
                Read(() => package.PublisherDisplayName),
                Read(() => package.Version),
                Read(() => package.InstallLocation),
                InstallDate: null,
                IsSystemComponent: Read(() => package.IsSystem),
                IsFramework: Read(() => package.IsFramework)));
        }

        return programs;
    }

    private static string? Read(Func<string> getter)
    {
        try
        {
            string value = getter();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static bool Read(Func<bool> getter)
    {
        try
        {
            return getter();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }
}

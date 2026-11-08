namespace LeftoverScout.Core.Evidence;

/// <summary>
/// The filters of the Programs page (decision D-037), as a pure function so that it can be tested without WPF.
/// </summary>
public static class ProgramFilter
{
    /// <summary>
    /// Returns the programs that match all filters, sorted by name (ordinal, ignoring case), then by source.
    /// </summary>
    /// <param name="programs">All collected programs.</param>
    /// <param name="search">Text that must occur in the name or publisher (ignoring case); empty matches everything.</param>
    /// <param name="source">Only this source; null means all sources.</param>
    /// <param name="includeSystemComponents">False hides system components and frameworks.</param>
    public static IReadOnlyList<InstalledProgram> Apply(
        IEnumerable<InstalledProgram> programs,
        string? search,
        ProgramSource? source,
        bool includeSystemComponents)
    {
        ArgumentNullException.ThrowIfNull(programs);

        string text = search?.Trim() ?? string.Empty;
        return programs
            .Where(p => source is null || p.Source == source)
            .Where(p => includeSystemComponents || !(p.IsSystemComponent || p.IsFramework))
            .Where(p => text.Length == 0
                || p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || (p.Publisher?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Source)
            .ToList();
    }

    /// <summary>Text for a source in the Source column and filter ("Program folder", "Start menu", ...).</summary>
    public static string SourceText(ProgramSource source) => source switch
    {
        ProgramSource.Registry => "Registry",
        ProgramSource.Store => "Store",
        ProgramSource.ProgramFolder => "Program folder",
        ProgramSource.StartMenu => "Start menu",
        _ => source.ToString(),
    };
}

using LeftoverScout.Core.Evidence;

namespace LeftoverScout.App.ViewModels;

/// <summary>
/// One entry of the Source filter on the Programs page.
/// </summary>
/// <param name="Label">Text shown in the filter.</param>
/// <param name="Source">The source to show; null means all sources.</param>
public sealed record SourceFilterOption(string Label, ProgramSource? Source)
{
    /// <summary>All options in display order: All, Registry, Store, Program folder, Start menu.</summary>
    public static IReadOnlyList<SourceFilterOption> All { get; } =
    [
        new("All", null),
        new(ProgramFilter.SourceText(ProgramSource.Registry), ProgramSource.Registry),
        new(ProgramFilter.SourceText(ProgramSource.Store), ProgramSource.Store),
        new(ProgramFilter.SourceText(ProgramSource.ProgramFolder), ProgramSource.ProgramFolder),
        new(ProgramFilter.SourceText(ProgramSource.StartMenu), ProgramSource.StartMenu),
    ];
}

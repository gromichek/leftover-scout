namespace LeftoverScout.Core.Rules;

/// <summary>
/// One validated rule of the knowledge base.
/// </summary>
/// <param name="Id">Unique kebab-case id.</param>
/// <param name="Kind">Ownership, cache or protected.</param>
/// <param name="Paths">One or more path patterns.</param>
/// <param name="Scope">Folder only, or the folder and everything below.</param>
/// <param name="Category">The cache category; set for cache rules only.</param>
/// <param name="Program">The program; required for ownership rules, optional for cache rules.</param>
/// <param name="Description">One short English sentence shown in the Results tooltip.</param>
/// <param name="Source">Where the knowledge comes from (not shown in the UI).</param>
public sealed record Rule(
    string Id,
    RuleKind Kind,
    IReadOnlyList<PathPattern> Paths,
    RuleScope Scope,
    CacheCategory? Category,
    ProgramHints? Program,
    string Description,
    string Source);

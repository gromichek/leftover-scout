using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Rules;

/// <summary>
/// The result of matching the rules against a scan: the matched rules of every folder that has any.
/// Computed once per completed scan and stored with the result.
/// </summary>
public sealed class RuleMatchMap
{
    private readonly Dictionary<FolderNode, FolderRuleMatch> _matches;

    internal RuleMatchMap(Dictionary<FolderNode, FolderRuleMatch> matches) => _matches = matches;

    /// <summary>A map without matches.</summary>
    public static RuleMatchMap Empty { get; } = new([]);

    /// <summary>Number of folders that have at least one matching rule.</summary>
    public int Count => _matches.Count;

    /// <summary>Returns the matches of <paramref name="folder"/>, or null when no rule applies.</summary>
    public FolderRuleMatch? Get(FolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return _matches.GetValueOrDefault(folder);
    }
}

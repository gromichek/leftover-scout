using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Classification;

/// <summary>
/// The leftover verdicts of one scan. Computed once per completed scan and stored with the result.
/// Folders that are not candidates have no entry.
/// </summary>
public sealed class LeftoverMap
{
    private readonly Dictionary<FolderNode, LeftoverVerdict> _verdicts;

    internal LeftoverMap(Dictionary<FolderNode, LeftoverVerdict> verdicts) => _verdicts = verdicts;

    /// <summary>A map without verdicts.</summary>
    public static LeftoverMap Empty { get; } = new([]);

    /// <summary>Number of folders that have a verdict.</summary>
    public int Count => _verdicts.Count;

    /// <summary>Returns the verdict of <paramref name="folder"/>, or null when it was not evaluated.</summary>
    public LeftoverVerdict? Get(FolderNode folder)
    {
        ArgumentNullException.ThrowIfNull(folder);
        return _verdicts.GetValueOrDefault(folder);
    }

    /// <summary>All folders that have a verdict.</summary>
    public IEnumerable<KeyValuePair<FolderNode, LeftoverVerdict>> Entries => _verdicts;
}

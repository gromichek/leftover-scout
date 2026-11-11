using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Tests.Cleanup;

/// <summary>Removes entries in memory: paths in <see cref="Locked"/> fail; removing a folder with a locked entry below it fails.</summary>
internal sealed class FakeEntryRemover : IEntryRemover
{
    public HashSet<string> Locked { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Vetoed { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Unexpected { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> Gone { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Calls { get; } = [];

    public EntryRemoveResult Remove(string path, CleanupMode mode)
    {
        Calls.Add(path);
        if (Vetoed.Contains(path))
        {
            return new EntryRemoveResult(EntryRemoveStatus.NotRecycled, "vetoed");
        }

        bool blocked = Locked.Any(locked => locked.Equals(path, StringComparison.OrdinalIgnoreCase)
            || locked.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase));
        if (blocked)
        {
            return new EntryRemoveResult(EntryRemoveStatus.Failed, "in use");
        }

        Gone.Add(path);
        return new EntryRemoveResult(EntryRemoveStatus.Removed, DeletedPermanentlyUnexpectedly: Unexpected.Contains(path));
    }
}

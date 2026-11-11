using LeftoverScout.Core.Cleanup;

namespace LeftoverScout.Core.Tests.Cleanup;

internal sealed class FakeFolderRemover : IFolderRemover
{
    public Dictionary<string, FolderRemoveResult> Results { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Removed { get; } = [];

    public Action<string>? OnRemove { get; set; }

    public FolderRemoveResult Remove(string path, CleanupMode mode)
    {
        Removed.Add(path);
        OnRemove?.Invoke(path);
        return Results.TryGetValue(path, out FolderRemoveResult? result) ? result : new FolderRemoveResult(FolderRemoveStatus.Removed);
    }
}

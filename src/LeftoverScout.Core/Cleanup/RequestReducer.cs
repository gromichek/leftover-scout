namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Reduces the selected items: duplicates are removed (case-insensitive, normalized paths) and an item inside
/// another selected item is dropped, because the outer item covers it. Pure text work.
/// </summary>
public static class RequestReducer
{
    /// <summary>Reduces <paramref name="items"/> and reports what was merged.</summary>
    public static RequestReduction Reduce(IReadOnlyList<CleanupItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // Items whose path cannot be parsed stay as they are (the guard blocks them); exact duplicates are still removed.
        var keys = new string[items.Count];
        var depths = new int[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            ParsedPath? parsed = ParsedPath.ParseOrNull(items[i].FullPath);
            keys[i] = parsed?.Key ?? "?" + items[i].FullPath;
            depths[i] = parsed?.Segments.Length ?? int.MaxValue;
        }

        // Shallow folders first, so that every ancestor is decided before the folders inside it.
        int[] order = [.. Enumerable.Range(0, items.Count).OrderBy(i => depths[i])];

        var kept = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var mergedAt = new Dictionary<int, MergedItem>();
        foreach (int index in order)
        {
            if (kept.TryGetValue(keys[index], out int sameIndex))
            {
                mergedAt[index] = new MergedItem(items[index], items[sameIndex].FullPath, MergeReason.Duplicate);
                continue;
            }

            int coveringIndex = depths[index] == int.MaxValue ? -1 : FindCoveringAncestor(items[index].FullPath, kept);
            if (coveringIndex >= 0)
            {
                mergedAt[index] = new MergedItem(items[index], items[coveringIndex].FullPath, MergeReason.InsideSelectedFolder);
                continue;
            }

            kept[keys[index]] = index;
        }

        var remaining = new List<CleanupItem>();
        var merged = new List<MergedItem>();
        for (int i = 0; i < items.Count; i++)
        {
            if (mergedAt.TryGetValue(i, out MergedItem? mergedItem))
            {
                merged.Add(mergedItem);
            }
            else
            {
                remaining.Add(items[i]);
            }
        }

        return new RequestReduction(remaining, merged);
    }

    private static int FindCoveringAncestor(string path, Dictionary<string, int> kept)
    {
        ParsedPath parsed = ParsedPath.ParseOrNull(path)!;
        for (int length = parsed.Segments.Length - 1; length >= 0; length--)
        {
            if (kept.TryGetValue(parsed.Prefix(length).Key, out int index))
            {
                return index;
            }
        }

        return -1;
    }
}

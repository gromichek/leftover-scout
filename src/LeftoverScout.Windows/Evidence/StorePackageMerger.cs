namespace LeftoverScout.Windows.Evidence;

/// <summary>Merges the results of several package queries and removes duplicates by full name.</summary>
internal static class StorePackageMerger
{
    /// <summary>Returns each package once (first occurrence wins, case-insensitive full name); packages without a full name are kept.</summary>
    public static List<T> Merge<T>(IEnumerable<IEnumerable<T>> results, Func<T, string?> fullName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<T>();
        foreach (IEnumerable<T> result in results)
        {
            foreach (T package in result)
            {
                string? name = fullName(package);
                if (string.IsNullOrEmpty(name) || seen.Add(name))
                {
                    merged.Add(package);
                }
            }
        }

        return merged;
    }
}

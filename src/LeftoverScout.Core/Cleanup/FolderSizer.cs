using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Adds up the logical size of what is left in a folder. Only lists folders (names, lengths and attributes); it never
/// opens a file, never follows links and does not enter online-only cloud folders.
/// </summary>
public static class FolderSizer
{
    /// <summary>Returns the total length in bytes of all files below <paramref name="path"/>; 0 when it cannot be read.</summary>
    public static long Measure(IDirectoryReader reader, string path)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        long total = 0;
        var pending = new Stack<string>();
        pending.Push(path);

        while (pending.Count > 0)
        {
            string folder = pending.Pop();
            foreach (DirectoryEntry entry in reader.ReadDirectory(folder).Entries)
            {
                bool isReparse = (entry.Attributes & EntryClassifier.AttributeReparsePoint) != 0;
                if (isReparse && EntryClassifier.IsNameSurrogate(entry.ReparseTag))
                {
                    continue;
                }

                if (!entry.IsDirectory)
                {
                    total += entry.Length;
                }
                else if ((entry.Attributes & EntryClassifier.AttributeRecallOnOpen) == 0)
                {
                    pending.Push(System.IO.Path.Combine(folder, entry.Name));
                }
            }
        }

        return total;
    }
}

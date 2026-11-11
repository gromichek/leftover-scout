using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Cleanup;

/// <summary>
/// Removes a folder. It first asks the operating system to remove the whole folder. If that fails (typically because
/// a file is in use), it removes the children one by one, depth-first, so that everything that is not in use is
/// removed and only locked entries (and their parent folders) remain (decision D-063). Links (junctions, symbolic
/// links) are never followed and never touched in this fallback: they are left in place and counted.
/// </summary>
public sealed class FolderRemover : IFolderRemover
{
    // Deeper trees are not walked; what lies deeper is counted as left. Far below the path length limit.
    private const int MaxDepth = 128;

    private readonly IEntryRemover _entries;
    private readonly IDirectoryReader _reader;

    /// <summary>Creates the remover.</summary>
    /// <param name="entries">Removes single entries through the operating system.</param>
    /// <param name="reader">Lists folders (names and attributes only).</param>
    public FolderRemover(IEntryRemover entries, IDirectoryReader reader)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(reader);
        _entries = entries;
        _reader = reader;
    }

    private sealed class Tally
    {
        public int Removed;
        public int Left;
        public bool Unexpected;
        public string? FirstReason;
    }

    /// <inheritdoc />
    public FolderRemoveResult Remove(string path, CleanupMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Measured before the first attempt: a permanent delete of a whole folder removes file by file and can stop
        // half way (for example at a file in use). What it removed before it stopped must count as progress.
        long sizeBefore = FolderSizer.Measure(_reader, path);

        EntryRemoveResult whole = _entries.Remove(path, mode);
        switch (whole.Status)
        {
            case EntryRemoveStatus.Removed:
                return new FolderRemoveResult(FolderRemoveStatus.Removed, DeletedPermanentlyUnexpectedly: whole.DeletedPermanentlyUnexpectedly);
            case EntryRemoveStatus.NotFound:
                return new FolderRemoveResult(FolderRemoveStatus.NotFound, Reason: "The folder does not exist any more.");
            case EntryRemoveStatus.NotRecycled:
                // The bin is not possible for this folder: trying the children could not change that.
                return new FolderRemoveResult(FolderRemoveStatus.NotRecycled, Reason: whole.Reason);
        }

        var tally = new Tally { Unexpected = whole.DeletedPermanentlyUnexpectedly, FirstReason = whole.Reason };
        RemoveContents(path, mode, tally, depth: 0);

        if (tally.Left == 0)
        {
            // Everything inside is gone: the empty folder itself can go now.
            EntryRemoveResult self = _entries.Remove(path, mode);
            tally.Unexpected |= self.DeletedPermanentlyUnexpectedly;
            if (self.Status is EntryRemoveStatus.Removed or EntryRemoveStatus.NotFound)
            {
                return new FolderRemoveResult(FolderRemoveStatus.Removed, DeletedPermanentlyUnexpectedly: tally.Unexpected);
            }

            tally.Left++;
            tally.FirstReason ??= self.Reason;
        }

        string reason = $"{tally.Left} item(s) could not be removed: {tally.FirstReason ?? "in use or not accessible"}.";
        bool anythingRemoved = tally.Removed > 0 || FolderSizer.Measure(_reader, path) < sizeBefore;
        return anythingRemoved
            ? new FolderRemoveResult(FolderRemoveStatus.PartlyRemoved, tally.Left, reason, tally.Unexpected)
            : new FolderRemoveResult(FolderRemoveStatus.Failed, tally.Left, reason, tally.Unexpected);
    }

    private void RemoveContents(string folder, CleanupMode mode, Tally tally, int depth)
    {
        if (depth >= MaxDepth)
        {
            tally.Left++;
            tally.FirstReason ??= "The folder tree is too deep.";
            return;
        }

        DirectoryListing listing = _reader.ReadDirectory(folder);
        if (listing.ErrorReason is not null)
        {
            tally.Left++;
            tally.FirstReason ??= listing.ErrorReason;
            return;
        }

        foreach (DirectoryEntry entry in listing.Entries)
        {
            string path = System.IO.Path.Combine(folder, entry.Name);

            bool isLink = (entry.Attributes & EntryClassifier.AttributeReparsePoint) != 0
                && EntryClassifier.IsNameSurrogate(entry.ReparseTag);
            if (isLink)
            {
                tally.Left++;
                tally.FirstReason ??= "A junction or symbolic link was left in place.";
                continue;
            }

            bool isCloudFolder = entry.IsDirectory && (entry.Attributes & EntryClassifier.AttributeRecallOnOpen) != 0;
            if (isCloudFolder)
            {
                tally.Left++;
                tally.FirstReason ??= "An online-only cloud folder was left in place.";
                continue;
            }

            // A subfolder is tried as a whole first; only when that fails do we go inside it.
            EntryRemoveResult result = _entries.Remove(path, mode);
            tally.Unexpected |= result.DeletedPermanentlyUnexpectedly;

            switch (result.Status)
            {
                case EntryRemoveStatus.Removed:
                    tally.Removed++;
                    break;
                case EntryRemoveStatus.NotFound:
                    break;
                case EntryRemoveStatus.NotRecycled:
                    tally.Left++;
                    tally.FirstReason ??= result.Reason;
                    break;
                default:
                    HandleFailed(path, entry, mode, tally, depth, result);
                    break;
            }
        }
    }

    private void HandleFailed(string path, DirectoryEntry entry, CleanupMode mode, Tally tally, int depth, EntryRemoveResult failure)
    {
        if (!entry.IsDirectory)
        {
            tally.Left++;
            tally.FirstReason ??= failure.Reason;
            return;
        }

        int leftBefore = tally.Left;
        RemoveContents(path, mode, tally, depth + 1);
        if (tally.Left != leftBefore)
        {
            // Something inside remains, so the folder cannot go; it is already counted through its content.
            return;
        }

        EntryRemoveResult self = _entries.Remove(path, mode);
        tally.Unexpected |= self.DeletedPermanentlyUnexpectedly;
        if (self.Status == EntryRemoveStatus.Removed)
        {
            tally.Removed++;
        }
        else if (self.Status != EntryRemoveStatus.NotFound)
        {
            tally.Left++;
            tally.FirstReason ??= self.Reason;
        }
    }
}

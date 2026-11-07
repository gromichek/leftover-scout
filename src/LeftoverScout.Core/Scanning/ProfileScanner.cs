namespace LeftoverScout.Core.Scanning;

/// <summary>
/// Walks a user profile read-only and builds a folder tree with aggregated sizes.
/// All file system access goes through <see cref="IDirectoryReader"/>; the rules for links and cloud files
/// live in <see cref="EntryClassifier"/>. Hard links are counted once per link.
/// </summary>
public sealed class ProfileScanner
{
    private readonly IDirectoryReader _reader;
    private readonly TimeProvider _time;

    /// <summary>Creates a scanner.</summary>
    /// <param name="reader">Lists folders.</param>
    /// <param name="timeProvider">Clock used for duration and progress throttling; the system clock when null.</param>
    public ProfileScanner(IDirectoryReader reader, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Scans the profile on a background thread. Cancellation is checked before every folder and ends the scan
    /// with <see cref="ScanCompletion.Cancelled"/> (no exception).
    /// </summary>
    /// <param name="target">The profile to scan.</param>
    /// <param name="options">Scan settings.</param>
    /// <param name="progress">Receives throttled progress reports; may be null.</param>
    /// <param name="cancellationToken">Cancels the scan.</param>
    public Task<ScanResult> ScanAsync(
        ScanTarget target,
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(options);

        // The token is observed inside Scan so that cancellation yields a result instead of an exception.
        return Task.Run(() => Scan(target, options, progress, cancellationToken), CancellationToken.None);
    }

    private ScanResult Scan(ScanTarget target, ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        long startTimestamp = _time.GetTimestamp();
        long lastReportTimestamp = startTimestamp;
        bool firstReport = true;

        var matcher = new PersonalFolderMatcher(target.ProfileRootPath, options.PersonalFolderPaths);
        var root = new FolderNode(target.ProfileRootPath, target.ProfileRootPath, parent: null);
        var discovered = new List<FolderNode> { root };
        var pending = new Stack<FolderNode>();
        pending.Push(root);

        var errors = new List<ScanError>();
        var largeFiles = new List<LargeFile>();
        int skippedLinks = 0;
        int cloudFolders = 0;
        long foldersScanned = 0;
        long filesScanned = 0;
        long localBytes = 0;

        while (pending.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ScanResult(ScanCompletion.Cancelled, null, [], [], skippedLinks, cloudFolders, 0, _time.GetElapsedTime(startTimestamp));
            }

            FolderNode node = pending.Pop();
            foldersScanned++;

            if (progress is not null && (firstReport || _time.GetElapsedTime(lastReportTimestamp) >= options.ProgressInterval))
            {
                firstReport = false;
                lastReportTimestamp = _time.GetTimestamp();
                progress.Report(new ScanProgress(node.FullPath, foldersScanned, filesScanned, localBytes));
            }

            DirectoryListing listing = _reader.ReadDirectory(node.FullPath);
            if (listing.ErrorReason is not null)
            {
                node.IsAccessDenied = listing.IsAccessDenied;
                node.HasError = !listing.IsAccessDenied;
                errors.Add(new ScanError(node.FullPath, listing.ErrorReason));
            }

            foreach (DirectoryEntry entry in listing.Entries)
            {
                switch (EntryClassifier.Classify(entry))
                {
                    case EntryKind.SkippedLink:
                        skippedLinks++;
                        if (entry.IsDirectory)
                        {
                            FolderNode link = AddChild(node, entry, matcher);
                            link.IsSkippedLink = true;
                        }

                        break;

                    case EntryKind.Directory:
                        FolderNode folder = AddChild(node, entry, matcher);
                        node.SubfolderCount++;
                        discovered.Add(folder);
                        pending.Push(folder);
                        break;

                    case EntryKind.CloudFolderNotScanned:
                        FolderNode cloudFolder = AddChild(node, entry, matcher);
                        cloudFolder.IsCloudFolderNotScanned = true;
                        node.SubfolderCount++;
                        cloudFolders++;
                        break;

                    case EntryKind.CloudOnlyFile:
                        node.AddFile(entry.Length, entry.LastWriteTimeUtc, isCloudOnly: true);
                        filesScanned++;
                        break;

                    default:
                        node.AddFile(entry.Length, entry.LastWriteTimeUtc, isCloudOnly: false);
                        filesScanned++;
                        localBytes += entry.Length;
                        if (entry.Length >= options.LargeFileThresholdBytes)
                        {
                            largeFiles.Add(new LargeFile(System.IO.Path.Combine(node.FullPath, entry.Name), entry.Length, entry.LastWriteTimeUtc));
                        }

                        break;
                }
            }
        }

        Aggregate(discovered);
        largeFiles.Sort(CompareLargeFiles);

        return new ScanResult(
            ScanCompletion.Completed,
            root,
            largeFiles,
            errors,
            skippedLinks,
            cloudFolders,
            SumPersonalSize(root),
            _time.GetElapsedTime(startTimestamp));
    }

    private static FolderNode AddChild(FolderNode parent, DirectoryEntry entry, PersonalFolderMatcher matcher)
    {
        string path = System.IO.Path.Combine(parent.FullPath, entry.Name);
        var child = new FolderNode(entry.Name, path, parent)
        {
            IsPersonal = matcher.IsPersonal(path),
        };
        parent.MutableChildren.Add(child);
        return child;
    }

    /// <summary>
    /// Adds every folder's totals to its parent. Parents are always discovered before their children, so walking
    /// the discovery order backwards finishes every folder before it is added to its parent.
    /// </summary>
    private static void Aggregate(List<FolderNode> discovered)
    {
        for (int i = discovered.Count - 1; i > 0; i--)
        {
            FolderNode node = discovered[i];
            node.Parent!.Absorb(node);
        }

        foreach (FolderNode node in discovered)
        {
            node.MutableChildren.Sort(CompareByName);
        }
    }

    private static int CompareByName(FolderNode left, FolderNode right)
    {
        int result = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return result != 0 ? result : string.CompareOrdinal(left.Name, right.Name);
    }

    private static int CompareLargeFiles(LargeFile left, LargeFile right)
    {
        int result = right.Size.CompareTo(left.Size);
        return result != 0 ? result : string.CompareOrdinal(left.Path, right.Path);
    }

    private static long SumPersonalSize(FolderNode root)
    {
        long total = 0;
        var stack = new Stack<FolderNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            FolderNode node = stack.Pop();
            if (node.IsPersonal)
            {
                // Nested personal folders are already inside this folder's size.
                total += node.LocalSize;
                continue;
            }

            foreach (FolderNode child in node.Children)
            {
                stack.Push(child);
            }
        }

        return total;
    }
}

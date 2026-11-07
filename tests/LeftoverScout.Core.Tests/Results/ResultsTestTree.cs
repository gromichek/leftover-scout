using LeftoverScout.Core.Scanning;
using static LeftoverScout.Core.Tests.Scanning.FakeDirectoryReader;

namespace LeftoverScout.Core.Tests.Results;

/// <summary>
/// Builds a small fictional scan result for the Results tests.
/// </summary>
internal static class ResultsTestTree
{
    public static ScanResult Scan(FakeDirectoryReader reader, IReadOnlyList<string>? personalFolders = null, long largeFileThreshold = 100L * 1024 * 1024)
    {
        var options = new ScanOptions { PersonalFolderPaths = personalFolders ?? [], LargeFileThresholdBytes = largeFileThreshold };
        return new ProfileScanner(reader)
            .ScanAsync(new ScanTarget(Root, true), options, null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Root has 50 bytes of own files; Big (700 B, 2 files, one subfolder Deep), Small (200 B), a link,
    /// a cloud folder, an unreadable folder and a personal folder Documents (50 B).
    /// </summary>
    public static ScanResult Sample()
    {
        var reader = new FakeDirectoryReader()
            .Folder(Root, DirEntry("Big"), DirEntry("Small"), DirEntry("Documents"), JunctionEntry("Application Data"), CloudDirEntry("OneDriveOnly"), DirEntry("Locked"), FileEntry("root.txt", 50, 2020))
            .Folder(Root + @"\Big", DirEntry("Deep"), FileEntry("big.bin", 300, 2023))
            .Folder(Root + @"\Big\Deep", FileEntry("deep.bin", 400, 2024))
            .Folder(Root + @"\Small", FileEntry("small.bin", 200, 2022))
            .Folder(Root + @"\Documents", FileEntry("doc.txt", 50, 2021))
            .Failing(Root + @"\Locked", "Access denied", accessDenied: true);
        return Scan(reader, [Root + @"\Documents"], largeFileThreshold: 300);
    }
}

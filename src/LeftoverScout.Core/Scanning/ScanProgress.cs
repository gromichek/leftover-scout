namespace LeftoverScout.Core.Scanning;

/// <summary>
/// A progress report of a running scan.
/// </summary>
/// <param name="CurrentFolder">Folder being read.</param>
/// <param name="FoldersScanned">Folders read so far.</param>
/// <param name="FilesScanned">Files found so far.</param>
/// <param name="LocalBytes">Local bytes found so far.</param>
public sealed record ScanProgress(string CurrentFolder, long FoldersScanned, long FilesScanned, long LocalBytes);

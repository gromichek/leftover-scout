namespace LeftoverScout.Core.Cleanup;

/// <summary>Progress of a cleanup session, reported before each item.</summary>
/// <param name="ItemIndex">Zero-based index of the current item.</param>
/// <param name="ItemCount">Number of items in the session.</param>
/// <param name="CurrentRelativePath">Path of the current item relative to the profile root.</param>
/// <param name="BytesDone">Bytes removed so far.</param>
public sealed record CleanupProgress(int ItemIndex, int ItemCount, string CurrentRelativePath, long BytesDone);

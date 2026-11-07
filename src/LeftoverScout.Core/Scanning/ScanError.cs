namespace LeftoverScout.Core.Scanning;

/// <summary>
/// A location that could not be read completely.
/// </summary>
/// <param name="Path">Path of the folder.</param>
/// <param name="Reason">Short reason.</param>
public sealed record ScanError(string Path, string Reason);

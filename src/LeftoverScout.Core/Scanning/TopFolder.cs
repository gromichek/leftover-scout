namespace LeftoverScout.Core.Scanning;

/// <summary>
/// A top-level folder of the profile with its local size.
/// </summary>
/// <param name="Name">Folder name.</param>
/// <param name="LocalSize">Local bytes in the subtree.</param>
public sealed record TopFolder(string Name, long LocalSize);

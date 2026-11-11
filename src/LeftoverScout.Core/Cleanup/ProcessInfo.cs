namespace LeftoverScout.Core.Cleanup;

/// <summary>A running process, as far as it is known without touching it.</summary>
/// <param name="Name">The process name, without path.</param>
/// <param name="ImagePath">The full path of the program file; null when it could not be read.</param>
public sealed record ProcessInfo(string Name, string? ImagePath);

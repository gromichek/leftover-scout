namespace LeftoverScout.Core.Evidence;

/// <summary>
/// A source that could not be read. The message is short and contains no paths.
/// </summary>
/// <param name="Source">The failing source.</param>
/// <param name="Message">Short message for the user, for example "Store apps could not be read."</param>
public sealed record EvidenceError(ProgramSource Source, string Message);

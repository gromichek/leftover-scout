namespace LeftoverScout.Core.Classification;

/// <summary>A folder name that matches installed-program evidence.</summary>
/// <param name="ProgramName">The installed program to show to the user.</param>
/// <param name="OnlyPublisher">True when the name matched only publisher names and no program, package or folder name.</param>
public sealed record EvidenceMatch(string ProgramName, bool OnlyPublisher);

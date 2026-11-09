using LeftoverScout.Core.Scanning;

namespace LeftoverScout.Core.Classification;

/// <summary>A folder that leftover detection evaluates.</summary>
/// <param name="Node">The folder.</param>
/// <param name="Kind">Where it was found.</param>
public sealed record LeftoverCandidate(FolderNode Node, CandidateKind Kind);

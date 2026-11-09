namespace LeftoverScout.Core.Classification;

/// <summary>
/// A name prepared for comparison (decision D-045): lower-case tokens without versions and company suffixes.
/// </summary>
/// <param name="Tokens">The tokens in their original order.</param>
/// <param name="Joined">The tokens written together without separators.</param>
public sealed record NormalizedName(IReadOnlyList<string> Tokens, string Joined)
{
    /// <summary>A name without tokens.</summary>
    public static NormalizedName Empty { get; } = new([], string.Empty);

    /// <summary>True when nothing is left after normalization.</summary>
    public bool IsEmpty => Tokens.Count == 0;
}

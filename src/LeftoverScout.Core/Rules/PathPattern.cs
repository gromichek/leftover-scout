using System.Diagnostics.CodeAnalysis;

namespace LeftoverScout.Core.Rules;

/// <summary>
/// A validated path pattern of a rule: one token (<c>{Profile}</c>, <c>{RoamingAppData}</c>, <c>{LocalAppData}</c>
/// or <c>{LocalLow}</c>) followed by folder names or <c>*</c> (exactly one whole folder name), separated by
/// backslashes. There are no regular expressions (decision D-041).
/// </summary>
public sealed class PathPattern
{
    private static readonly Dictionary<string, string[]> Tokens = new(StringComparer.Ordinal)
    {
        ["{Profile}"] = [],
        ["{RoamingAppData}"] = ["AppData", "Roaming"],
        ["{LocalAppData}"] = ["AppData", "Local"],
        ["{LocalLow}"] = ["AppData", "LocalLow"],
    };

    private const string InvalidSegmentCharacters = "{}%/:<>\"|?";

    private PathPattern(string text, string[] segments)
    {
        Text = text;
        Segments = segments;
        LiteralCount = segments.Count(segment => segment != "*");
    }

    /// <summary>The pattern as written in the rule file.</summary>
    public string Text { get; }

    /// <summary>The folder names below the profile root, with the token already expanded. <c>*</c> stands for any one name.</summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary>How many segments are literal names; a higher number means a more specific pattern.</summary>
    public int LiteralCount { get; }

    /// <summary>Parses and validates a pattern.</summary>
    /// <param name="text">The pattern text.</param>
    /// <param name="pattern">The parsed pattern when valid.</param>
    /// <param name="error">A short English description of the problem when invalid.</param>
    /// <returns>True when the pattern is valid.</returns>
    public static bool TryParse(string? text, [NotNullWhen(true)] out PathPattern? pattern, [NotNullWhen(false)] out string? error)
    {
        pattern = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "The path pattern is empty.";
            return false;
        }

        string[] parts = text.Split('\\');
        if (!Tokens.TryGetValue(parts[0], out string[]? tokenSegments))
        {
            error = parts[0].StartsWith('{') && parts[0].EndsWith('}')
                ? $"Unknown token '{parts[0]}' in '{text}'."
                : $"The path '{text}' must start with one of the tokens {{Profile}}, {{RoamingAppData}}, {{LocalAppData}} or {{LocalLow}}.";
            return false;
        }

        var segments = new List<string>(tokenSegments);
        foreach (string part in parts.Skip(1))
        {
            if (part.Length == 0)
            {
                error = $"The path '{text}' has an empty segment.";
                return false;
            }

            if (part is "." or "..")
            {
                error = $"The path '{text}' must not contain '.' or '..'.";
                return false;
            }

            if (part != "*" && part.Contains('*'))
            {
                error = $"The path '{text}' has a partial wildcard '{part}'; '*' must be a whole segment.";
                return false;
            }

            if (part.IndexOfAny(InvalidSegmentCharacters.ToCharArray()) >= 0)
            {
                error = $"The path '{text}' has the invalid segment '{part}'.";
                return false;
            }

            segments.Add(part);
        }

        pattern = new PathPattern(text, [.. segments]);
        error = null;
        return true;
    }

    /// <summary>
    /// True when this pattern could match a folder that lies at or below a folder matched by <paramref name="ancestor"/>.
    /// A <c>*</c> in this pattern counts as possibly matching.
    /// </summary>
    public bool MayLieInside(PathPattern ancestor)
    {
        ArgumentNullException.ThrowIfNull(ancestor);

        if (Segments.Count < ancestor.Segments.Count)
        {
            return false;
        }

        for (int i = 0; i < ancestor.Segments.Count; i++)
        {
            string theirs = ancestor.Segments[i];
            string mine = Segments[i];
            if (theirs != "*" && mine != "*" && !string.Equals(theirs, mine, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override string ToString() => Text;
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LeftoverScout.Core.Classification;

/// <summary>
/// Normalizes program and folder names for comparison (decision D-045). Pure; no fuzzy similarity.
/// </summary>
/// <remarks>
/// Steps: lower case; split into tokens on every character that is not a letter or digit and on camel-case and
/// letter-digit boundaries; drop version tokens (digits only, <c>v2</c>, <c>x64</c>, <c>x86</c>) and company suffixes
/// (<c>inc</c>, <c>ltd</c>, <c>llc</c>, ...). Dotted abbreviations such as <c>s.r.o.</c> are written together first.
/// </remarks>
public static partial class NameNormalizer
{
    private static readonly HashSet<string> CompanySuffixes = new(StringComparer.Ordinal)
    {
        "inc", "ltd", "llc", "corp", "corporation", "gmbh", "co", "sro", "ag",
    };

    // "s.r.o." -> "sro.", "L.L.C." -> "LLC.": a single letter, a dot, and another single letter.
    [GeneratedRegex(@"\b([A-Za-z])\.(?=[A-Za-z]\b)")]
    private static partial Regex DottedAbbreviation();

    // A whole word that is only a version: 2022, v2, x64, x86.
    [GeneratedRegex(@"^(?:\d+|v\d+|x64|x86)$", RegexOptions.IgnoreCase)]
    private static partial Regex VersionWord();

    /// <summary>Normalizes <paramref name="text"/>; null or empty text gives <see cref="NormalizedName.Empty"/>.</summary>
    public static NormalizedName Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return NormalizedName.Empty;
        }

        string prepared = DottedAbbreviation().Replace(text, "$1");
        var tokens = new List<string>();
        var word = new StringBuilder();

        foreach (char c in prepared)
        {
            if (char.IsLetterOrDigit(c))
            {
                word.Append(c);
            }
            else
            {
                AddWord(word, tokens);
            }
        }

        AddWord(word, tokens);
        return tokens.Count == 0 ? NormalizedName.Empty : new NormalizedName(tokens, string.Concat(tokens));
    }

    private static void AddWord(StringBuilder word, List<string> tokens)
    {
        if (word.Length == 0)
        {
            return;
        }

        string text = word.ToString();
        word.Clear();

        // Versions and company suffixes are dropped as whole words, before camel-case splitting
        // would cut them apart ("GmbH" -> "gmb" + "h").
        if (VersionWord().IsMatch(text) || CompanySuffixes.Contains(text.ToLower(CultureInfo.InvariantCulture)))
        {
            return;
        }

        int start = 0;
        for (int i = 1; i <= text.Length; i++)
        {
            if (i == text.Length || IsBoundary(text, i))
            {
                AddToken(text[start..i], tokens);
                start = i;
            }
        }
    }

    // Boundary between text[index - 1] and text[index]: lower->upper, letter<->digit, or the end of an acronym ("JSONParser").
    private static bool IsBoundary(string text, int index)
    {
        char previous = text[index - 1];
        char current = text[index];

        if (char.IsDigit(previous) != char.IsDigit(current))
        {
            return true;
        }

        if (char.IsLower(previous) && char.IsUpper(current))
        {
            return true;
        }

        return char.IsUpper(previous) && char.IsUpper(current) && index + 1 < text.Length && char.IsLower(text[index + 1]);
    }

    private static void AddToken(string raw, List<string> tokens)
    {
        string token = raw.ToLower(CultureInfo.InvariantCulture);
        if (token.All(char.IsDigit) || CompanySuffixes.Contains(token))
        {
            return;
        }

        tokens.Add(token);
    }
}

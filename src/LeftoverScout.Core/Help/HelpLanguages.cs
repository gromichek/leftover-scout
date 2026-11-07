using System.Globalization;

namespace LeftoverScout.Core.Help;

/// <summary>
/// Converts between <see cref="HelpLanguage"/>, its settings value and folder name, and picks the language to show.
/// </summary>
public static class HelpLanguages
{
    /// <summary>Settings value and folder name of English.</summary>
    public const string EnglishCode = "en";

    /// <summary>Settings value and folder name of Russian.</summary>
    public const string RussianCode = "ru";

    /// <summary>Returns the two-letter code (also the folder name) of a language.</summary>
    public static string ToCode(HelpLanguage language) =>
        language == HelpLanguage.Russian ? RussianCode : EnglishCode;

    /// <summary>
    /// Chooses the help language (decision D-028). A saved <paramref name="setting"/> of <c>en</c> or <c>ru</c> wins;
    /// otherwise (null, empty or unknown) the language follows <paramref name="uiCulture"/>: Russian gives Russian,
    /// anything else gives English.
    /// </summary>
    /// <param name="setting">The saved value, or null for automatic.</param>
    /// <param name="uiCulture">The Windows UI language.</param>
    public static HelpLanguage Resolve(string? setting, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(uiCulture);

        if (string.Equals(setting, RussianCode, StringComparison.OrdinalIgnoreCase))
        {
            return HelpLanguage.Russian;
        }

        if (string.Equals(setting, EnglishCode, StringComparison.OrdinalIgnoreCase))
        {
            return HelpLanguage.English;
        }

        return string.Equals(uiCulture.TwoLetterISOLanguageName, RussianCode, StringComparison.OrdinalIgnoreCase)
            ? HelpLanguage.Russian
            : HelpLanguage.English;
    }
}

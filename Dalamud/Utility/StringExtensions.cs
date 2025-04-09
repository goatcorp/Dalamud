using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Dalamud.Game;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for strings.
/// </summary>
public static class StringExtensions
{
    private static readonly string[] CommonExcludedWords = ["sas", "zos", "van", "nan", "tol", "deus", "mal", "de", "rem", "out", "yae", "bas", "cen", "quo", "viator", "la"];
    private static readonly string[] EnglishExcludedWords = ["of", "the", "to", "and", "a", "an", "or", "at", "by", "for", "in", "on", "with", "from", .. CommonExcludedWords];

    /// <summary>
    /// An extension method to chain usage of string.Format.
    /// </summary>
    /// <param name="format">Format string.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>Formatted string.</returns>
    public static string Format(this string format, params object[] args) => string.Format(format, args);

    /// <summary>
    /// Indicates whether the specified string is null or an empty string ("").
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <returns>true if the value parameter is null or an empty string (""); otherwise, false.</returns>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Indicates whether a specified string is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <returns>true if the value parameter is null or an empty string (""), or if value consists exclusively of white-space characters.</returns>
    public static bool IsNullOrWhitespace([NotNullWhen(false)] this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Validate if character name is valid using game check.
    /// </summary>
    /// <param name="value">character name to validate.</param>
    /// <param name="includeLegacy">include legacy names (combined can be 30 instead of 20).</param>
    /// <returns>indicator if character is name is valid.</returns>
    public static bool IsValidCharacterName(this string value, bool includeLegacy = true)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (!UIGlobals.IsValidPlayerCharacterName(value)) return false;
        return includeLegacy || value.Length <= 21;
    }

    /// <summary>
    /// Converts the first character of the string to uppercase while leaving the rest of the string unchanged.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="culture"><inheritdoc cref="string.ToLower(CultureInfo)" path="/param[@name='cultureInfo']"/></param>
    /// <returns>A new string with the first character converted to uppercase.</returns>
    [return: NotNullIfNotNull("input")]
    public static string? FirstCharToUpper(this string? input, CultureInfo? culture = null) =>
        string.IsNullOrWhiteSpace(input)
            ? input
            : $"{char.ToUpper(input[0], culture ?? CultureInfo.CurrentCulture)}{input.AsSpan(1)}";

    /// <summary>
    /// Converts the first character of the string to lowercase while leaving the rest of the string unchanged.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="culture"><inheritdoc cref="string.ToLower(CultureInfo)" path="/param[@name='cultureInfo']"/></param>
    /// <returns>A new string with the first character converted to lowercase.</returns>
    [return: NotNullIfNotNull("input")]
    public static string? FirstCharToLower(this string? input, CultureInfo? culture = null) =>
        string.IsNullOrWhiteSpace(input)
            ? input
            : $"{char.ToLower(input[0], culture ?? CultureInfo.CurrentCulture)}{input.AsSpan(1)}";

    /// <summary>
    /// Removes soft hyphen characters (U+00AD) from the input string.
    /// </summary>
    /// <param name="input">The input string to remove soft hyphen characters from.</param>
    /// <returns>A string with all soft hyphens removed.</returns>
    public static string StripSoftHyphen(this string input) => input.Replace("\u00AD", string.Empty);

    /// <summary>
    /// Truncates the given string to the specified maximum number of characters,  
    /// appending an ellipsis if truncation occurs.
    /// </summary>
    /// <param name="input">The string to truncate.</param>
    /// <param name="maxChars">The maximum allowed length of the string.</param>
    /// <param name="ellipses">The string to append if truncation occurs (defaults to "...").</param>
    /// <returns>The truncated string, or the original string if no truncation is needed.</returns>
    public static string? Truncate(this string input, int maxChars, string ellipses = "...")
    {
        return string.IsNullOrEmpty(input) || input.Length <= maxChars ? input : input[..maxChars] + ellipses;
    }

    /// <summary>
    /// Converts the input string to uppercase based on specified options like capitalizing the first character,
    /// normalizing vowels, and excluding certain words based on the selected language.
    /// </summary>
    /// <param name="input">The input string to be converted to uppercase.</param>
    /// <param name="firstCharOnly">Whether to capitalize only the first character of the string.</param>
    /// <param name="everyWord">Whether to capitalize the first letter of each word.</param>
    /// <param name="normalizeVowels">Whether to normalize vowels to uppercase if they appear at the beginning of a word.</param>
    /// <param name="language">The language context used to determine which words to exclude from capitalization.</param>
    /// <returns>A new string with the appropriate characters converted to uppercase.</returns>
    public static string ToUpper(this string input, bool firstCharOnly, bool everyWord, bool normalizeVowels, ClientLanguage language)
    {
        return ToUpper(input, firstCharOnly, everyWord, normalizeVowels, language switch
        {
            ClientLanguage.Japanese => [],
            ClientLanguage.English => EnglishExcludedWords,
            ClientLanguage.German => CommonExcludedWords,
            ClientLanguage.French => CommonExcludedWords,
            _ => [],
        });
    }

    /// <summary>
    /// Converts the input string to uppercase based on specified options like capitalizing the first character,
    /// normalizing vowels, and excluding certain words based on the selected language.
    /// </summary>
    /// <param name="input">The input string to be converted to uppercase.</param>
    /// <param name="firstCharOnly">Whether to capitalize only the first character of the string.</param>
    /// <param name="everyWord">Whether to capitalize the first letter of each word.</param>
    /// <param name="normalizeVowels">Whether to normalize vowels to uppercase if they appear at the beginning of a word.</param>
    /// <param name="excludedWords">A list of words to exclude from being capitalized. Words in this list will remain lowercase.</param>
    /// <returns>A new string with the appropriate characters converted to uppercase.</returns>
    public static string ToUpper(this string input, bool firstCharOnly, bool everyWord, bool normalizeVowels, ReadOnlySpan<string> excludedWords)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var builder = new StringBuilder(input);
        var isWordBeginning = true;
        var length = firstCharOnly && !everyWord ? 1 : builder.Length;

        for (var i = 0; i < length; i++)
        {
            var ch = builder[i];

            if (ch == ' ')
            {
                isWordBeginning = true;
                continue;
            }

            if (firstCharOnly && !isWordBeginning)
                continue;

            // Basic ASCII a-z
            if (ch >= 'a' && ch <= 'z')
            {
                var substr = builder.ToString(i, builder.Length - i);
                var isExcluded = false;

                // Do not exclude words at the beginning
                if (i > 0)
                {
                    foreach (var excludedWord in excludedWords)
                    {
                        if (substr.StartsWith(excludedWord + " ", StringComparison.OrdinalIgnoreCase))
                        {
                            isExcluded = true;
                            break;
                        }
                    }
                }

                if (!isExcluded)
                {
                    builder[i] = char.ToUpperInvariant(ch);
                }
            }

            // Special œ → Œ
            else if (ch == 'œ')
            {
                builder[i] = 'Œ';
            }

            // Characters with accents
            else if (ch >= 'à' && ch <= 'ý' && ch != '÷')
            {
                builder[i] = char.ToUpperInvariant(ch);
            }

            // Normalize vowels with accents
            else if (normalizeVowels && isWordBeginning)
            {
                if ("àáâãäå".Contains(ch))
                {
                    builder[i] = 'A';
                }
                else if ("èéêë".Contains(ch))
                {
                    builder[i] = 'E';
                }
                else if ("ìíîï".Contains(ch))
                {
                    builder[i] = 'I';
                }
                else if ("òóôõö".Contains(ch))
                {
                    builder[i] = 'O';
                }
                else if ("ùúûü".Contains(ch))
                {
                    builder[i] = 'U';
                }
            }

            isWordBeginning = false;
        }

        return builder.ToString();
    }
}

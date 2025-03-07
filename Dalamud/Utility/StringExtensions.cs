using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for strings.
/// </summary>
public static class StringExtensions
{
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
}

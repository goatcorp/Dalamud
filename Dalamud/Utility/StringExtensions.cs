using System.Diagnostics.CodeAnalysis;

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
}

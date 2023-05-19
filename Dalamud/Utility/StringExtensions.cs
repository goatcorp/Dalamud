using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
    /// Validate if character name is valid.
    /// Both forename and surname must be between 2 and 15 characters and not total more than 20 characters combined.
    /// Only letters, hyphens, and apostrophes can be used.
    /// The first character of either name must be a letter.
    /// Hyphens cannot be used in succession or placed immediately before or after apostrophes.
    /// </summary>
    /// <param name="value">character name to validate.</param>
    /// <returns>indicator if character is name is valid.</returns>
    public static bool IsValidCharacterName(this string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        if (value.Length > 21) return false; // add 1 to allow for space
        var names = value.Split(' ');
        if (names.Length != 2) return false;
        var forename = names[0];
        var surname = names[1];
        if (!IsValidName(forename)) return false;
        if (!IsValidName(surname)) return false;
        return true;
    }

    private static bool IsValidName(string name)
    {
        if (name.Length is < 2 or > 15) return false;
        if (name.Any(c => !char.IsLetter(c) && !c.Equals('\'') && !c.Equals('-'))) return false;
        if (!char.IsLetter(name[0])) return false;
        if (!char.IsUpper(name[0])) return false;
        if (name.Contains("--")) return false;
        if (name.Contains("\'-")) return false;
        if (name.Contains("-\'")) return false;
        return true;
    }
}

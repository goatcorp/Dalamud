using System.Collections.Generic;

namespace Dalamud.Game.Text.Sanitizer;

/// <summary>
/// Sanitize strings to remove soft hyphens and other special characters.
/// </summary>
public interface ISanitizer
{
    /// <summary>
    /// Creates a sanitized string using current clientLanguage.
    /// </summary>
    /// <param name="unsanitizedString">An unsanitized string to sanitize.</param>
    /// <returns>A sanitized string.</returns>
    string Sanitize(string unsanitizedString);

    /// <summary>
    /// Creates a sanitized string using request clientLanguage.
    /// </summary>
    /// <param name="unsanitizedString">An unsanitized string to sanitize.</param>
    /// <param name="clientLanguage">Target language for sanitized strings.</param>
    /// <returns>A sanitized string.</returns>
    string Sanitize(string unsanitizedString, ClientLanguage clientLanguage);

    /// <summary>
    /// Creates a list of sanitized strings using current clientLanguage.
    /// </summary>
    /// <param name="unsanitizedStrings">List of unsanitized string to sanitize.</param>
    /// <returns>A list of sanitized strings.</returns>
    IEnumerable<string> Sanitize(IEnumerable<string> unsanitizedStrings);

    /// <summary>
    /// Creates a list of sanitized strings using requested clientLanguage.
    /// </summary>
    /// <param name="unsanitizedStrings">List of unsanitized string to sanitize.</param>
    /// <param name="clientLanguage">Target language for sanitized strings.</param>
    /// <returns>A list of sanitized strings.</returns>
    IEnumerable<string> Sanitize(IEnumerable<string> unsanitizedStrings, ClientLanguage clientLanguage);
}

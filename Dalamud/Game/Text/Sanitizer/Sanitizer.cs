using System.Collections.Generic;
using System.Linq;

namespace Dalamud.Game.Text.Sanitizer;

/// <summary>
/// Sanitize strings to remove soft hyphens and other special characters.
/// </summary>
public class Sanitizer : ISanitizer
{
    private static readonly Dictionary<string, string> DESanitizationDict = new()
    {
        { "\u0020\u2020", string.Empty }, // dagger
    };

    private static readonly Dictionary<string, string> FRSanitizationDict = new()
    {
        { "\u0153", "\u006F\u0065" }, // ligature oe
    };

    private readonly ClientLanguage defaultClientLanguage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Sanitizer"/> class.
    /// </summary>
    /// <param name="defaultClientLanguage">Default clientLanguage for sanitizing strings.</param>
    public Sanitizer(ClientLanguage defaultClientLanguage)
    {
        this.defaultClientLanguage = defaultClientLanguage;
    }

    /// <summary>
    /// Creates a sanitized string using current clientLanguage.
    /// </summary>
    /// <param name="unsanitizedString">An unsanitized string to sanitize.</param>
    /// <returns>A sanitized string.</returns>
    public string Sanitize(string unsanitizedString)
    {
        return SanitizeByLanguage(unsanitizedString, this.defaultClientLanguage);
    }

    /// <summary>
    /// Creates a sanitized string using request clientLanguage.
    /// </summary>
    /// <param name="unsanitizedString">An unsanitized string to sanitize.</param>
    /// <param name="clientLanguage">Target language for sanitized strings.</param>
    /// <returns>A sanitized string.</returns>
    public string Sanitize(string unsanitizedString, ClientLanguage clientLanguage)
    {
        return SanitizeByLanguage(unsanitizedString, clientLanguage);
    }

    /// <summary>
    /// Creates a list of sanitized strings using current clientLanguage.
    /// </summary>
    /// <param name="unsanitizedStrings">List of unsanitized string to sanitize.</param>
    /// <returns>A list of sanitized strings.</returns>
    public IEnumerable<string> Sanitize(IEnumerable<string> unsanitizedStrings)
    {
        return SanitizeByLanguage(unsanitizedStrings, this.defaultClientLanguage);
    }

    /// <summary>
    /// Creates a list of sanitized strings using requested clientLanguage.
    /// </summary>
    /// <param name="unsanitizedStrings">List of unsanitized string to sanitize.</param>
    /// <param name="clientLanguage">Target language for sanitized strings.</param>
    /// <returns>A list of sanitized strings.</returns>
    public IEnumerable<string> Sanitize(IEnumerable<string> unsanitizedStrings, ClientLanguage clientLanguage)
    {
        return SanitizeByLanguage(unsanitizedStrings, clientLanguage);
    }

    private static string SanitizeByLanguage(string unsanitizedString, ClientLanguage clientLanguage)
    {
        var sanitizedString = FilterUnprintableCharacters(unsanitizedString);
        return clientLanguage switch
        {
            ClientLanguage.Japanese or ClientLanguage.English => sanitizedString,
            ClientLanguage.German => FilterByDict(sanitizedString, DESanitizationDict),
            ClientLanguage.French => FilterByDict(sanitizedString, FRSanitizationDict),
            _ => throw new ArgumentOutOfRangeException(nameof(clientLanguage), clientLanguage, null),
        };
    }

    private static IEnumerable<string> SanitizeByLanguage(IEnumerable<string> unsanitizedStrings, ClientLanguage clientLanguage)
    {
        return clientLanguage switch
        {
            ClientLanguage.Japanese => unsanitizedStrings.Select(FilterUnprintableCharacters),
            ClientLanguage.English => unsanitizedStrings.Select(FilterUnprintableCharacters),
            ClientLanguage.German => unsanitizedStrings.Select(original => FilterByDict(FilterUnprintableCharacters(original), DESanitizationDict)),
            ClientLanguage.French => unsanitizedStrings.Select(original => FilterByDict(FilterUnprintableCharacters(original), FRSanitizationDict)),
            _ => throw new ArgumentOutOfRangeException(nameof(clientLanguage), clientLanguage, null),
        };
    }

    private static string FilterUnprintableCharacters(string str)
    {
        return new string(str?.Where(ch => ch >= 0x20).ToArray());
    }

    private static string FilterByDict(string str, Dictionary<string, string> dict)
    {
        return dict.Aggregate(
            str, (current, kvp) =>
                current.Replace(kvp.Key, kvp.Value));
    }
}

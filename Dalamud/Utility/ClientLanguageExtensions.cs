using Dalamud.Game;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for the <see cref="ClientLanguage"/> class.
/// </summary>
public static class ClientLanguageExtensions
{
    /// <summary>
    /// Converts a Dalamud ClientLanguage to the corresponding Lumina variant.
    /// </summary>
    /// <param name="language">Language to convert.</param>
    /// <returns>Converted language.</returns>
    public static Lumina.Data.Language ToLumina(this ClientLanguage language)
    {
        return language switch
        {
            ClientLanguage.Japanese => Lumina.Data.Language.Japanese,
            ClientLanguage.English => Lumina.Data.Language.English,
            ClientLanguage.German => Lumina.Data.Language.German,
            ClientLanguage.French => Lumina.Data.Language.French,
            _ => throw new ArgumentOutOfRangeException(nameof(language)),
        };
    }

    /// <summary>
    /// Gets the language code from a ClientLanguage.
    /// </summary>
    /// <param name="value">The ClientLanguage to convert.</param>
    /// <returns>The language code (ja, en, de, fr).</returns>
    /// <exception cref="ArgumentOutOfRangeException">An exception that is thrown when no valid ClientLanguage was given.</exception>
    public static string ToCode(this ClientLanguage value)
    {
        return value switch
        {
            ClientLanguage.Japanese => "ja",
            ClientLanguage.English => "en",
            ClientLanguage.German => "de",
            ClientLanguage.French => "fr",
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };
    }

    /// <summary>
    /// Gets the ClientLanguage from a language code.
    /// </summary>
    /// <param name="value">The language code to convert (ja, en, de, fr).</param>
    /// <returns>The ClientLanguage.</returns>
    /// <exception cref="ArgumentOutOfRangeException">An exception that is thrown when no valid language code was given.</exception>
    public static ClientLanguage ToClientLanguage(this string value)
    {
        return value switch
        {
            "ja" => ClientLanguage.Japanese,
            "en" => ClientLanguage.English,
            "de" => ClientLanguage.German,
            "fr" => ClientLanguage.French,
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };
    }
}

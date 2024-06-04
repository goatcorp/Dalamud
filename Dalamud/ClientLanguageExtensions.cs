using Dalamud.Utility;

namespace Dalamud;

/// <summary>
/// Extension methods for the <see cref="ClientLanguage"/> class.
/// </summary>
[Api10ToDo("Delete this, and use Dalamud.Common.ClientLanguage instead for everything.")]
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
}

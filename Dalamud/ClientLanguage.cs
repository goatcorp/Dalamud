using Dalamud.Utility;

namespace Dalamud;

/// <summary>
/// Enum describing the language the game loads in.
/// </summary>
[Api10ToDo("Delete this, and use Dalamud.Common.ClientLanguage instead for everything.")]
public enum ClientLanguage
{
    /// <summary>
    /// Indicating a Japanese game client.
    /// </summary>
    Japanese,

    /// <summary>
    /// Indicating an English game client.
    /// </summary>
    English,

    /// <summary>
    /// Indicating a German game client.
    /// </summary>
    German,

    /// <summary>
    /// Indicating a French game client.
    /// </summary>
    French,
}

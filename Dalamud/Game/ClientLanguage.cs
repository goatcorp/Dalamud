using Dalamud.Utility;

namespace Dalamud.Game;

/// <summary>
/// Enum describing the language the game loads in.
/// </summary>
[Api16ToDo("Use Dalamud.Common.ClientLanguage")]
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

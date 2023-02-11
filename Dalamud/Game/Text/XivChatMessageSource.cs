namespace Dalamud.Game.Text;

/// <summary>
/// The original source of a chat log message.  This is purely a Dalamud construct.
/// </summary>
public enum XivChatMessageSource
{
    /// <summary>
    /// The message originated from an unknown source.  Seeing this source probably means there's a bug somewhere in Dalamud.
    /// </summary>
    Unknown,

    /// <summary>
    /// The message originated from the game itself (or at least from outside the purview of Dalamud).
    /// </summary>
    Game,

    /// <summary>
    /// The message originated from within Dalamud.
    /// </summary>
    Dalamud,

    /// <summary>
    /// The message originated from within a plugin managed by Dalamud.
    /// </summary>
    Plugin,

    // ***** TODO: Should we have additional entries for these sources to indicate that they were modified by XYZ?
}

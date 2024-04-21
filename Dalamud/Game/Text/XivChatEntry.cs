using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Text;

/// <summary>
/// This class represents a single chat log entry.
/// </summary>
public sealed class XivChatEntry
{
    /// <summary>
    /// Gets or sets the type of entry.
    /// </summary>
    public XivChatType Type { get; set; } = XivChatType.Debug;

    /// <summary>
    /// Gets or sets the sender ID.
    /// </summary>
    public uint SenderId { get; set; }

    /// <summary>
    /// Gets or sets the sender name.
    /// </summary>
    public SeString Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public SeString Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message parameters.
    /// </summary>
    public IntPtr Parameters { get; set; }
}

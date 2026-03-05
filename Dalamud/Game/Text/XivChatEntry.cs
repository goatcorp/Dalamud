using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Text;

/// <summary>
/// This class represents a single chat log entry.
/// </summary>
public sealed class XivChatEntry // TODO: implement IChatMessage
{
    /// <summary>
    /// Gets or sets the type of entry.
    /// </summary>
    public XivChatType? Type { get; set; }

    /// <summary>
    /// Gets or sets the message timestamp.
    /// </summary>
    public int Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sender name.
    /// </summary>
    public ReadOnlySeString Name { get; set; }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public ReadOnlySeString Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether new message sounds should be silenced or not.
    /// </summary>
    public bool Silent { get; set; }
}

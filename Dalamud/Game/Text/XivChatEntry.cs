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
    public XivChatType? Type { get; set; }

    /// <summary>
    /// Gets or sets the message timestamp.
    /// </summary>
    public int Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sender name.
    /// </summary>
    public SeString Name
    {
        get => SeString.Parse(this.NameBytes);
        set => this.NameBytes = value.Encode();
    }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public SeString Message
    {
        get => SeString.Parse(this.MessageBytes);
        set => this.MessageBytes = value.Encode();
    }

    /// <summary>
    /// Gets or sets the name payloads.
    /// </summary>
    public byte[] NameBytes { get; set; } = [];

    /// <summary>
    /// Gets or sets the message payloads.
    /// </summary>
    public byte[] MessageBytes { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether new message sounds should be silenced or not.
    /// </summary>
    public bool Silent { get; set; }
}

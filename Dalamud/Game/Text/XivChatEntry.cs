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
    public SeString Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public SeString Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether new message sounds should be silenced or not.
    /// </summary>
    public bool Silent { get; set; }
}

/// <summary>
/// This class represents a readonly chat message ready to be posted.
/// </summary>
public struct XivChatEntryReadOnly
{
    /// <summary>
    /// Gets the type of entry.
    /// </summary>
    public XivChatType? Type { get; }

    /// <summary>
    /// Gets the message timestamp.
    /// </summary>
    public int Timestamp { get; }

    /// <summary>
    /// Gets the sender name.
    /// </summary>
    public byte[] Name { get; }

    /// <summary>
    /// Gets the message.
    /// </summary>
    public byte[] Message { get; }

    /// <summary>
    /// Gets a value indicating whether new message sounds should be silenced or not.
    /// </summary>
    public bool Silent { get; }

    public XivChatEntryReadOnly(byte[] message, byte[]? name = null, XivChatType? type = null, int timestamp = 0, bool silent = false)
    {
        this.Type = type;
        this.Timestamp = timestamp;
        this.Name = name ?? [];
        this.Message = message;
        this.Silent = silent;
    }
}

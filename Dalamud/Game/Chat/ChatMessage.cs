using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Chat;

#pragma warning disable SA1500 // Braces for multi-line statements should not share line
#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <summary>
/// Interface representing a chat message.
/// </summary>
public interface IChatMessage : IEquatable<IChatMessage>
{
    /// <summary>
    /// Gets the type of chat.
    /// </summary>
    XivChatType LogKind { get; }

    /// <summary>
    /// Gets the relationship of the entity sending the message or performing the action.
    /// </summary>
    XivChatRelationKind SourceKind { get; }

    /// <summary>
    /// Gets the relationship of the entity receiving the message or being targeted by the action.
    /// </summary>
    XivChatRelationKind TargetKind { get; }

    /// <summary>
    /// Gets the timestamp of when the message was sent.
    /// </summary>
    int Timestamp { get; }

    /// <summary>
    /// Gets the sender name.
    /// </summary>
    SeString Sender { get; }

    /// <summary>
    /// Gets the message sent.
    /// </summary>
    SeString Message { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Sender"/> was modified by a plugin.
    /// </summary>
    bool SenderModified { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Message"/> was modified by a plugin.
    /// </summary>
    bool MessageModified { get; }

    /// <summary>
    /// Gets a value indicating whether the message was handled by a plugin.
    /// </summary>
    bool IsHandled { get; }
}

/// <summary>
/// Interface representing a chat message that can be modified by a plugin.
/// </summary>
public interface IModifyableChatMessage : IChatMessage
{
    /// <summary>
    /// Gets or sets the sender name.
    /// </summary>
    new SeString Sender { get; set; }

    /// <summary>
    /// Gets or sets the message sent.
    /// </summary>
    new SeString Message { get; set; }
}

/// <summary>
/// Interface representing a chat message that can be handled by a plugin.
/// </summary>
public interface IHandleableChatMessage : IModifyableChatMessage
{
    /// <summary>
    /// Marks this message as handled (<see cref="IChatMessage.IsHandled"/> = <see langword="true"/>) and prevents it from being processed by the game any further.
    /// </summary>
    void PreventOriginal();
}

/// <summary>
/// This struct represents an intercepted chat message.
/// </summary>
internal class ChatMessage(XivChatType logKind, XivChatRelationKind sourceKind, XivChatRelationKind targetKind, SeString sender, SeString message, int timestamp) : IHandleableChatMessage
{
    /// <inheritdoc />
    public XivChatType LogKind { get; } = logKind;

    /// <inheritdoc />
    public XivChatRelationKind SourceKind { get; } = sourceKind;

    /// <inheritdoc />
    public XivChatRelationKind TargetKind { get; } = targetKind;

    /// <inheritdoc />
    public SeString Sender
    {
        get;
        set
        {
            if (!field.Encode().SequenceEqual(value.Encode()))
            {
                field = value;
                this.SenderModified = true;
            }
        }
    } = sender;

    /// <inheritdoc />
    public SeString Message
    {
        get;
        set
        {
            if (!field.Encode().SequenceEqual(value.Encode()))
            {
                field = value;
                this.MessageModified = true;
            }
        }
    } = message;

    /// <inheritdoc />
    public bool SenderModified { get; private set; }

    /// <inheritdoc />
    public bool MessageModified { get; private set; }

    /// <inheritdoc />
    public int Timestamp { get; } = timestamp;

    /// <inheritdoc />
    public bool IsHandled { get; private set; }

    /// <inheritdoc />
    public void PreventOriginal() => this.IsHandled = true;

    /// <inheritdoc />
    public bool Equals(IChatMessage? other)
    {
        return other != null
            && (ReferenceEquals(this, other)
            || (this.LogKind == other.LogKind
                && this.SourceKind == other.SourceKind
                && this.TargetKind == other.TargetKind
                && this.Sender == other.Sender
                && this.Message == other.Message
                && this.Timestamp == other.Timestamp));
    }
}

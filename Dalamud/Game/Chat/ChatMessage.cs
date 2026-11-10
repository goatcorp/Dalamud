using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Chat;

#pragma warning disable SA1500 // Braces for multi-line statements should not share line
#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <summary>
/// Interface representing a chat message.
/// </summary>
public interface IChatMessage
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
    /// Gets the original sender name, before any plugin might have changed it.
    /// </summary>
    ReadOnlySeString OriginalSender { get; }

    /// <summary>
    /// Gets the original message, before any plugin might have changed it.
    /// </summary>
    ReadOnlySeString OriginalMessage { get; }

    /// <summary>
    /// Gets the sender name.
    /// </summary>
    SeString Sender { get; }

    /// <summary>
    /// Gets the message sent.
    /// </summary>
    SeString Message { get; }

    /// <summary>
    /// Gets the timestamp of when the message was sent.
    /// </summary>
    int Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether the message was handled by a plugin.
    /// </summary>
    bool IsHandled { get; }
}

/// <summary>
/// Interface representing a chat message that can be modified by a plugin.
/// </summary>
public interface IMutableChatMessage : IChatMessage
{
    /// <summary>
    /// Gets or sets the sender name.
    /// </summary>
    new SeString Sender { get; set; }

    /// <summary>
    /// Gets or sets the message sent.
    /// </summary>
    new SeString Message { get; set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Sender"/> was modified by a plugin.
    /// </summary>
    bool SenderModified { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Message"/> was modified by a plugin.
    /// </summary>
    bool MessageModified { get; }
}

/// <summary>
/// Interface representing a chat message that can be handled by a plugin.
/// </summary>
public interface IHandleableChatMessage : IMutableChatMessage
{
    /// <summary>
    /// Marks this message as handled (<see cref="IChatMessage.IsHandled"/> = <see langword="true"/>) and prevents it from being processed by the game any further.
    /// </summary>
    void PreventOriginal();
}

/// <summary>
/// This struct represents an intercepted chat message.
/// </summary>
internal unsafe class ChatMessage : IHandleableChatMessage
{
    private Utf8String* senderPointer;
    private Utf8String* messagePointer;
    private ReadOnlySeString? cachedOriginalSender;
    private ReadOnlySeString? cachedOriginalMessage;
    private SeString? cachedSender;
    private SeString? cachedMessage;

    /// <inheritdoc />
    public XivChatType LogKind { get; private set; }

    /// <inheritdoc />
    public XivChatRelationKind SourceKind { get; private set; }

    /// <inheritdoc />
    public XivChatRelationKind TargetKind { get; private set; }

    /// <inheritdoc />
    public ReadOnlySeString OriginalSender => this.cachedOriginalSender ??= this.senderPointer->AsReadOnlySeString();

    /// <inheritdoc />
    public ReadOnlySeString OriginalMessage => this.cachedOriginalMessage ??= this.messagePointer->AsReadOnlySeString();

    /// <inheritdoc />
    public SeString Sender
    {
        get => this.cachedSender ??= this.senderPointer != null ? this.senderPointer->AsDalamudSeString() : new SeString();
        set => this.cachedSender = value;
    }

    /// <inheritdoc />
    public SeString Message
    {
        get => this.cachedMessage ??= this.messagePointer != null ? this.messagePointer->AsDalamudSeString() : new SeString();
        set => this.cachedMessage = value;
    }

    /// <inheritdoc />
    public bool SenderModified
    {
        get
        {
            if (this.cachedSender == null)
                return false;

            if (!field)
            {
                var encoded = this.Sender.Encode();
                field = new ReadOnlySeStringSpan(encoded) != this.senderPointer->AsReadOnlySeStringSpan();
            }
            return field;
        }
        private set;
    }

    /// <inheritdoc />
    public bool MessageModified
    {
        get
        {
            if (this.cachedMessage == null)
                return false;

            if (!field)
            {
                var encoded = this.Message.Encode();
                return new ReadOnlySeStringSpan(encoded) != this.messagePointer->AsReadOnlySeStringSpan();
            }
            return field;
        }
        private set;
    }

    /// <inheritdoc />
    public int Timestamp { get; private set; }

    /// <inheritdoc />
    public bool IsHandled { get; private set; }

    /// <inheritdoc />
    public void PreventOriginal() => this.IsHandled = true;

    /// <summary>
    /// Sets data for a new chat message, allowing the object to be reused.
    /// </summary>
    /// <param name="logKind">The type of chat.</param>
    /// <param name="sourceKind">The relationship of the entity sending the message or performing the action.</param>
    /// <param name="targetKind">The relationship of the entity receiving the message or being targeted by the action.</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="timestamp">The timestamp of when the message was sent.</param>
    internal void SetData(XivChatType logKind, XivChatRelationKind sourceKind, XivChatRelationKind targetKind, Utf8String* sender, Utf8String* message, int timestamp)
    {
        this.senderPointer = sender;
        this.messagePointer = message;
        this.cachedOriginalSender = null;
        this.cachedOriginalMessage = null;
        this.cachedSender = null;
        this.cachedMessage = null;

        this.LogKind = logKind;
        this.SourceKind = sourceKind;
        this.TargetKind = targetKind;
        this.Timestamp = timestamp;
        this.IsHandled = false;
    }

    /// <summary>
    /// Clears all data of this object.
    /// </summary>
    internal void Clear()
    {
        this.senderPointer = null;
        this.messagePointer = null;
        this.cachedOriginalSender = null;
        this.cachedOriginalMessage = null;
        this.cachedSender = null;
        this.cachedMessage = null;

        this.LogKind = 0;
        this.SourceKind = 0;
        this.TargetKind = 0;
        this.Timestamp = 0;
        this.IsHandled = false;
    }
}

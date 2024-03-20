using System.Collections.Generic;

using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
public interface IChatGui
{
    /// <summary>
    /// A delegate type used with the <see cref="ChatGui.ChatMessage"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="senderId">The sender ID.</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="isHandled">A value indicating whether the message was handled or should be propagated.</param>
    public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui.CheckMessageHandled"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="senderId">The sender ID.</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="isHandled">A value indicating whether the message was handled or should be propagated.</param>
    public delegate void OnCheckMessageHandledDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui.ChatMessageHandled"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="senderId">The sender ID.</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    public delegate void OnMessageHandledDelegate(XivChatType type, uint senderId, SeString sender, SeString message);

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui.ChatMessageUnhandled"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="senderId">The sender ID.</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    public delegate void OnMessageUnhandledDelegate(XivChatType type, uint senderId, SeString sender, SeString message);
    
    /// <summary>
    /// Event that will be fired when a chat message is sent to chat by the game.
    /// </summary>
    public event OnMessageDelegate ChatMessage;

    /// <summary>
    /// Event that allows you to stop messages from appearing in chat by setting the isHandled parameter to true.
    /// </summary>
    public event OnCheckMessageHandledDelegate CheckMessageHandled;

    /// <summary>
    /// Event that will be fired when a chat message is handled by Dalamud or a Plugin.
    /// </summary>
    public event OnMessageHandledDelegate ChatMessageHandled;

    /// <summary>
    /// Event that will be fired when a chat message is not handled by Dalamud or a Plugin.
    /// </summary>
    public event OnMessageUnhandledDelegate ChatMessageUnhandled;
    
    /// <summary>
    /// Gets the ID of the last linked item.
    /// </summary>
    public int LastLinkedItemId { get; }
    
    /// <summary>
    /// Gets the flags of the last linked item.
    /// </summary>
    public byte LastLinkedItemFlags { get; }
    
    /// <summary>
    /// Gets the dictionary of Dalamud Link Handlers.
    /// </summary>
    public IReadOnlyDictionary<(string PluginName, uint CommandId), Action<uint, SeString>> RegisteredLinkHandlers { get; }

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="chat">A message to send.</param>
    public void Print(XivChatEntry chat);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    public void Print(string message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    public void Print(SeString message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    public void PrintError(string message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    public void PrintError(SeString message, string? messageTag = null, ushort? tagColor = null);
}

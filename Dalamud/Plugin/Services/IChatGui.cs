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
    /// Queue a chat message. While method is named as PrintChat, it only add a entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="chat">A message to send.</param>
    public void PrintChat(XivChatEntry chat);

    /// <summary>
    /// Queue a chat message. While method is named as PrintChat (it calls it internally), it only add a entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    public void Print(string message);

    /// <summary>
    /// Queue a chat message. While method is named as PrintChat (it calls it internally), it only add a entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    public void Print(SeString message);

    /// <summary>
    /// Queue an error chat message. While method is named as PrintChat (it calls it internally), it only add a entry to
    /// the queue, later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    public void PrintError(string message);

    /// <summary>
    /// Queue an error chat message. While method is named as PrintChat (it calls it internally), it only add a entry to
    /// the queue, later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    public void PrintError(SeString message);

    /// <summary>
    /// Process a chat queue.
    /// </summary>
    public void UpdateQueue();
}

using System.Collections.Generic;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
public interface IChatGui : IDalamudService
{
    /// <summary>
    /// A delegate type used for the <see cref="ChatMessage"/> and <see cref="CheckMessageHandled"/> events.
    /// </summary>
    /// <param name="message">The message sent.</param>
    delegate void OnHandleableChatMessageDelegate(IHandleableChatMessage message);

    /// <summary>
    /// A delegate type used for the <see cref="ChatMessageHandled"/> and <see cref="ChatMessageUnhandled"/> events.
    /// </summary>
    /// <param name="message">The message sent.</param>
    delegate void OnChatMessageDelegate(IChatMessage message);

    /// <summary>
    /// A delegate type used for the <see cref="LogMessage"/> event.
    /// </summary>
    /// <param name="message">The message sent. The passed object is only valid during the event callback and must not be used after returning from it.</param>
    delegate void OnLogMessageDelegate(ILogMessage message);

    /// <summary>
    /// An event that will be fired when a chat message is sent to chat by the game.
    /// </summary>
    event OnHandleableChatMessageDelegate ChatMessage;

    /// <summary>
    /// A follow-up event after <see cref="ChatMessage"/>, that allows for final modifications, like translation or formatting.<br/>
    /// It is only fired if the message was not suppressed during the <see cref="ChatMessage"/> event.
    /// </summary>
    event OnHandleableChatMessageDelegate CheckMessageHandled;

    /// <summary>
    /// Event that will be fired when a chat message is handled by Dalamud or a Plugin.
    /// </summary>
    event OnChatMessageDelegate ChatMessageHandled;

    /// <summary>
    /// Event that will be fired when a chat message is not handled by Dalamud or a Plugin.
    /// </summary>
    event OnChatMessageDelegate ChatMessageUnhandled;

    /// <summary>
    /// Event that will be fired when a log message, that is a chat message based on entries in the LogMessage sheet, is sent.
    /// </summary>
    event OnLogMessageDelegate LogMessage;

    /// <summary>
    /// Gets the ID of the last linked item.
    /// </summary>
    uint LastLinkedItemId { get; }

    /// <summary>
    /// Gets the flags of the last linked item.
    /// </summary>
    byte LastLinkedItemFlags { get; }

    /// <summary>
    /// Gets the dictionary of Dalamud Link Handlers.
    /// </summary>
    IReadOnlyDictionary<(string PluginName, uint CommandId), Action<uint, SeString>> RegisteredLinkHandlers { get; }

    /// <summary>
    /// Register a chat link handler.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    /// <param name="commandAction">The action to be executed.</param>
    /// <returns>Returns an SeString payload for the link.</returns>
    DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction);

    /// <summary>
    /// Remove a chat link handler.
    /// </summary>
    /// <param name="commandId">The ID of the command.</param>
    void RemoveChatLinkHandler(uint commandId);

    /// <summary>
    /// Removes all chat link handlers registered by the plugin.
    /// </summary>
    void RemoveChatLinkHandler();

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="chat">A message to send.</param>
    void Print(XivChatEntry chat);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    void Print(string message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    void Print(SeString message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    void PrintError(string message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    void PrintError(SeString message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    void Print(ReadOnlySpan<byte> message, string? messageTag = null, ushort? tagColor = null);

    /// <summary>
    /// Queue a chat message. Dalamud will send queued messages on the next framework event.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="messageTag">String to prepend message with "[messageTag] ".</param>
    /// <param name="tagColor">Color to display the message tag with.</param>
    void PrintError(ReadOnlySpan<byte> message, string? messageTag = null, ushort? tagColor = null);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Libc;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game.Gui;

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
[PluginInterface]
[InterfaceVersion("2.0")]
[ServiceManager.BlockingEarlyLoadedService]
public sealed class ChatGui2 : IDisposable, IServiceType
{
    private readonly ChatGuiAddressResolver address;

    private readonly Queue<XivChatEntry2> chatQueue = new();
    private readonly Dictionary<(string PluginName, uint CommandId), Action<uint, SeString>> dalamudLinkHandlers = new();

    private readonly Hook<PrintMessageDelegate> printMessageHook;
    private readonly Hook<PopulateItemLinkDelegate> populateItemLinkHook;
    private readonly Hook<InteractableLinkClickedDelegate> interactableLinkClickedHook;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    [ServiceManager.ServiceDependency]
    private readonly LibcFunction libcFunction = Service<LibcFunction>.Get();

    private IntPtr baseAddress = IntPtr.Zero;

    [ServiceManager.ServiceConstructor]
    private ChatGui2(SigScanner sigScanner)
    {
        this.address = new ChatGuiAddressResolver();
        this.address.Setup(sigScanner);

        this.printMessageHook = Hook<PrintMessageDelegate>.FromAddress(this.address.PrintMessage, this.PrintMessageDetour);
        this.populateItemLinkHook = Hook<PopulateItemLinkDelegate>.FromAddress(this.address.PopulateItemLinkObject, this.HandlePopulateItemLinkDetour);
        this.interactableLinkClickedHook = Hook<InteractableLinkClickedDelegate>.FromAddress(this.address.InteractableLinkClicked, this.InteractableLinkClickedDetour);
    }

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui2.ChatMessage"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="timestamp">The unix timestamp (0 means automatic).</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="source">The source of the message (game, Dalamud, or plugin).</param>
    /// <param name="sourceName">The name of the message source.  This is the plugin name when the source is a plugin.</param>
    /// <param name="isHandled">A value indicating whether the message was handled or should be propagated.</param>
    public delegate void OnMessageDelegate(XivChatType2 type, uint timestamp, ref SeString sender, ref SeString message, XivChatMessageSource source, string sourceName, ref bool isHandled);

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui2.CheckMessageHandled"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="timestamp">The unix timestamp (0 means automatic).</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="source">The source of the message (game, Dalamud, or plugin).</param>
    /// <param name="sourceName">The name of the message source.  This is the plugin name when the source is a plugin.</param>
    /// <param name="isHandled">A value indicating whether the message was handled or should be propagated.</param>
    public delegate void OnCheckMessageHandledDelegate(XivChatType2 type, uint timestamp, ref SeString sender, ref SeString message, XivChatMessageSource source, string sourceName, ref bool isHandled);

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui2.ChatMessageHandled"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="timestamp">The unix timestamp (0 means automatic).</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="source">The source of the message (game, Dalamud, or plugin).</param>
    /// <param name="sourceName">The name of the message source.  This is the plugin name when the source is a plugin.</param>
    public delegate void OnMessageHandledDelegate(XivChatType2 type, uint timestamp, SeString sender, SeString message, XivChatMessageSource source, string sourceName);

    /// <summary>
    /// A delegate type used with the <see cref="ChatGui2.ChatMessageUnhandled"/> event.
    /// </summary>
    /// <param name="type">The type of chat.</param>
    /// <param name="timestamp">The unix timestamp (0 means automatic).</param>
    /// <param name="sender">The sender name.</param>
    /// <param name="message">The message sent.</param>
    /// <param name="source">The source of the message (game, Dalamud, or plugin).</param>
    /// <param name="sourceName">The name of the message source.  This is the plugin name when the source is a plugin.</param>
    public delegate void OnMessageUnhandledDelegate(XivChatType2 type, uint timestamp, SeString sender, SeString message, XivChatMessageSource source, string sourceName);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr PrintMessageDelegate(IntPtr manager, XivChatType2 chatType, IntPtr senderName, IntPtr message, uint timestamp, IntPtr parameter);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void PopulateItemLinkDelegate(IntPtr linkObjectPtr, IntPtr itemInfoPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void InteractableLinkClickedDelegate(IntPtr managerPtr, IntPtr messagePtr);

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
    public int LastLinkedItemId { get; private set; }

    /// <summary>
    /// Gets the flags of the last linked item.
    /// </summary>
    public byte LastLinkedItemFlags { get; private set; }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        this.printMessageHook.Dispose();
        this.populateItemLinkHook.Dispose();
        this.interactableLinkClickedHook.Dispose();
    }

    /// <summary>
    /// Queue a chat message. While method is named as PrintChat, it only adds an entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="chat">A message to send.</param>
    /// <param name="pluginInterface">Your plugin's <see cref="DalamudPluginInterface"/>.</param>
    public void PrintChat(XivChatEntry2 chat, DalamudPluginInterface pluginInterface)
    {
        this.PrintChat(chat, XivChatMessageSource.Plugin, pluginInterface?.InternalName);
    }

    /// <summary>
    /// Queue a chat message. While method is named as Print, it only adds an entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="pluginInterface">Your plugin's <see cref="DalamudPluginInterface"/>.</param>
    public void Print(string message, DalamudPluginInterface pluginInterface)
    {
        // Log.Verbose("[CHATGUI PRINT REGULAR]{0}", message);
        this.PrintChat(
            new XivChatEntry2
            {
                Message = message,
                Type = this.configuration.GeneralChatType,
            },
            pluginInterface);
    }

    /// <summary>
    /// Queue a chat message. While method is named as Print, it only adds an entry to the queue,
    /// later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="pluginInterface">Your plugin's <see cref="DalamudPluginInterface"/>.</param>
    public void Print(SeString message, DalamudPluginInterface pluginInterface)
    {
        // Log.Verbose("[CHATGUI PRINT SESTRING]{0}", message.TextValue);
        this.PrintChat(
            new XivChatEntry2
            {
                Message = message,
                Type = this.configuration.GeneralChatType,
            },
            pluginInterface);
    }

    /// <summary>
    /// Queue an error chat message. While method is named as Print, it only adds an entry to
    /// the queue, later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="pluginInterface">Your plugin's <see cref="DalamudPluginInterface"/>.</param>
    public void PrintError(string message, DalamudPluginInterface pluginInterface)
    {
        // Log.Verbose("[CHATGUI PRINT REGULAR ERROR]{0}", message);
        this.PrintChat(
            new XivChatEntry2
            {
                Message = message,
                Type = XivChatType2.Urgent,
            },
            pluginInterface);
    }

    /// <summary>
    /// Queue an error chat message. While method is named as Print, it only adds an entry to
    /// the queue, later to be processed when UpdateQueue() is called.
    /// </summary>
    /// <param name="message">A message to send.</param>
    /// <param name="pluginInterface">Your plugin's <see cref="DalamudPluginInterface"/>.</param>
    public void PrintError(SeString message, DalamudPluginInterface pluginInterface)
    {
        // Log.Verbose("[CHATGUI PRINT SESTRING ERROR]{0}", message.TextValue);
        this.PrintChat(
            new XivChatEntry2
            {
                Message = message,
                Type = XivChatType2.Urgent,
            },
            pluginInterface);
    }

    /// <summary>
    /// Process a chat queue.
    /// </summary>
    public void UpdateQueue()
    {
        while (this.chatQueue.Count > 0)
        {
            var chat = this.chatQueue.Dequeue();

            if (this.baseAddress == IntPtr.Zero)
            {
                continue;
            }

            var senderRaw = (chat.Name ?? string.Empty).Encode();
            using var senderOwned = this.libcFunction.NewString(senderRaw);

            var messageRaw = (chat.Message ?? string.Empty).Encode();
            using var messageOwned = this.libcFunction.NewString(messageRaw);

            this.HandlePrintMessage(this.baseAddress, chat.Type, senderOwned.Address, messageOwned.Address, chat.Timestamp, chat.Parameters, chat.MessageSource, chat.SourceName);
        }
    }

    /// <summary>
    /// Queue a chat message. This is the internal version that should be used by all Dalamud calls to set the message source.
    /// </summary>
    /// <param name="chat">A message to send.</param>
    /// <param name="source">The ultimate source of the chat message (game, Dalamud, or plugin).</param>
    /// <param name="sourceName">The name of the source of the chat message (i.e., generally the plugin name).  This parameter can be omitted if the source is not a plugin.</param>
    internal void PrintChat(XivChatEntry2 chat, XivChatMessageSource source, string? sourceName = null)
    {
        sourceName ??= source switch
        {
            XivChatMessageSource.Game => "Game",
            XivChatMessageSource.Dalamud => "Dalamud",
            XivChatMessageSource.Plugin => string.Empty,
            _ => string.Empty,
        };

        chat.MessageSource = source;
        chat.SourceName = sourceName;
        this.chatQueue.Enqueue(chat);
    }

    /// <summary>
    /// Create a link handler.
    /// </summary>
    /// <param name="pluginName">The name of the plugin handling the link.</param>
    /// <param name="commandId">The ID of the command to run.</param>
    /// <param name="commandAction">The command action itself.</param>
    /// <returns>A payload for handling.</returns>
    internal DalamudLinkPayload AddChatLinkHandler(string pluginName, uint commandId, Action<uint, SeString> commandAction)
    {
        var payload = new DalamudLinkPayload() { Plugin = pluginName, CommandId = commandId };
        this.dalamudLinkHandlers.Add((pluginName, commandId), commandAction);
        return payload;
    }

    /// <summary>
    /// Remove all handlers owned by a plugin.
    /// </summary>
    /// <param name="pluginName">The name of the plugin handling the links.</param>
    internal void RemoveChatLinkHandler(string pluginName)
    {
        foreach (var handler in this.dalamudLinkHandlers.Keys.ToList().Where(k => k.PluginName == pluginName))
        {
            this.dalamudLinkHandlers.Remove(handler);
        }
    }

    /// <summary>
    /// Remove a registered link handler.
    /// </summary>
    /// <param name="pluginName">The name of the plugin handling the link.</param>
    /// <param name="commandId">The ID of the command to be removed.</param>
    internal void RemoveChatLinkHandler(string pluginName, uint commandId)
    {
        if (this.dalamudLinkHandlers.ContainsKey((pluginName, commandId)))
        {
            this.dalamudLinkHandlers.Remove((pluginName, commandId));
        }
    }

    [ServiceManager.CallWhenServicesReady]
    private void ContinueConstruction(GameGui gameGui, LibcFunction libcFunction)
    {
        this.printMessageHook.Enable();
        this.populateItemLinkHook.Enable();
        this.interactableLinkClickedHook.Enable();
    }

    private void HandlePopulateItemLinkDetour(IntPtr linkObjectPtr, IntPtr itemInfoPtr)
    {
        try
        {
            this.populateItemLinkHook.Original(linkObjectPtr, itemInfoPtr);

            this.LastLinkedItemId = Marshal.ReadInt32(itemInfoPtr, 8);
            this.LastLinkedItemFlags = Marshal.ReadByte(itemInfoPtr, 0x14);

            // Log.Verbose($"HandlePopulateItemLinkDetour {linkObjectPtr} {itemInfoPtr} - linked:{this.LastLinkedItemId}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception onPopulateItemLink hook.");
            this.populateItemLinkHook.Original(linkObjectPtr, itemInfoPtr);
        }
    }

    private IntPtr PrintMessageDetour(IntPtr manager, XivChatType2 chattype, IntPtr pSenderName, IntPtr pMessage, uint timestamp, IntPtr parameter)
    {
        return this.HandlePrintMessage(manager, chattype, pSenderName, pMessage, timestamp, parameter, XivChatMessageSource.Game, "Game");
    }

    private IntPtr HandlePrintMessage(IntPtr manager, XivChatType2 chattype, IntPtr pSenderName, IntPtr pMessage, uint timestamp, IntPtr parameter, XivChatMessageSource source, string sourceName)
    {
        var retVal = IntPtr.Zero;

        try
        {
            var sender = StdString.ReadFromPointer(pSenderName);
            var parsedSender = SeString.Parse(sender.RawData);
            var originalSenderData = (byte[])sender.RawData.Clone();
            var oldEditedSender = parsedSender.Encode();
            var senderPtr = pSenderName;
            OwnedStdString allocatedString = null;

            var message = StdString.ReadFromPointer(pMessage);
            var parsedMessage = SeString.Parse(message.RawData);
            var originalMessageData = (byte[])message.RawData.Clone();
            var oldEdited = parsedMessage.Encode();
            var messagePtr = pMessage;
            OwnedStdString allocatedStringSender = null;

            // Log.Verbose("[CHATGUI][{0}][{1}]", parsedSender.TextValue, parsedMessage.TextValue);

            // Log.Debug($"HandlePrintMessage {manager} - [{chattype}] [{BitConverter.ToString(message.RawData).Replace("-", " ")}] {message.Value} from {senderName.Value}");

            // Call events
            var isHandled = false;

            var invocationList = this.CheckMessageHandled.GetInvocationList();
            foreach (var @delegate in invocationList)
            {
                try
                {
                    var messageHandledDelegate = @delegate as OnCheckMessageHandledDelegate;
                    messageHandledDelegate!.Invoke(chattype, timestamp, ref parsedSender, ref parsedMessage, source, sourceName, ref isHandled);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not invoke registered OnCheckMessageHandledDelegate for {Name}", @delegate.Method.Name);
                }
            }

            if (!isHandled)
            {
                invocationList = this.ChatMessage.GetInvocationList();
                foreach (var @delegate in invocationList)
                {
                    try
                    {
                        var messageHandledDelegate = @delegate as OnMessageDelegate;
                        messageHandledDelegate!.Invoke(chattype, timestamp, ref parsedSender, ref parsedMessage, source, sourceName, ref isHandled);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not invoke registered OnMessageDelegate for {Name}", @delegate.Method.Name);
                    }
                }
            }

            var newEdited = parsedMessage.Encode();
            if (!Util.FastByteArrayCompare(oldEdited, newEdited))
            {
                Log.Verbose("SeString was edited, taking precedence over StdString edit.");
                message.RawData = newEdited;
                // Log.Debug($"\nOLD: {BitConverter.ToString(originalMessageData)}\nNEW: {BitConverter.ToString(newEdited)}");
            }

            if (!Util.FastByteArrayCompare(originalMessageData, message.RawData))
            {
                allocatedString = this.libcFunction.NewString(message.RawData);
                Log.Debug($"HandlePrintMessage String modified: {originalMessageData}({messagePtr}) -> {message}({allocatedString.Address})");
                messagePtr = allocatedString.Address;
            }

            var newEditedSender = parsedSender.Encode();
            if (!Util.FastByteArrayCompare(oldEditedSender, newEditedSender))
            {
                Log.Verbose("SeString was edited, taking precedence over StdString edit.");
                sender.RawData = newEditedSender;
                // Log.Debug($"\nOLD: {BitConverter.ToString(originalMessageData)}\nNEW: {BitConverter.ToString(newEdited)}");
            }

            if (!Util.FastByteArrayCompare(originalSenderData, sender.RawData))
            {
                allocatedStringSender = this.libcFunction.NewString(sender.RawData);
                Log.Debug(
                    $"HandlePrint Sender modified: {originalSenderData}({senderPtr}) -> {sender}({allocatedStringSender.Address})");
                senderPtr = allocatedStringSender.Address;
            }

            // Print the original chat if it's handled.
            if (isHandled)
            {
                this.ChatMessageHandled?.Invoke(chattype, timestamp, parsedSender, parsedMessage, source, sourceName);
            }
            else
            {
                retVal = this.printMessageHook.Original(manager, chattype, senderPtr, messagePtr, timestamp, parameter);
                this.ChatMessageUnhandled?.Invoke(chattype, timestamp, parsedSender, parsedMessage, source, sourceName);
            }

            if (this.baseAddress == IntPtr.Zero)
                this.baseAddress = manager;

            allocatedString?.Dispose();
            allocatedStringSender?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on OnChatMessage hook.");
            retVal = this.printMessageHook.Original(manager, chattype, pSenderName, pMessage, timestamp, parameter);
        }

        return retVal;
    }

    private void InteractableLinkClickedDetour(IntPtr managerPtr, IntPtr messagePtr)
    {
        try
        {
            var interactableType = (Payload.EmbeddedInfoType)(Marshal.ReadByte(messagePtr, 0x1B) + 1);

            if (interactableType != Payload.EmbeddedInfoType.DalamudLink)
            {
                this.interactableLinkClickedHook.Original(managerPtr, messagePtr);
                return;
            }

            Log.Verbose($"InteractableLinkClicked: {Payload.EmbeddedInfoType.DalamudLink}");

            var payloadPtr = Marshal.ReadIntPtr(messagePtr, 0x10);
            var messageSize = 0;
            while (Marshal.ReadByte(payloadPtr, messageSize) != 0) messageSize++;
            var payloadBytes = new byte[messageSize];
            Marshal.Copy(payloadPtr, payloadBytes, 0, messageSize);
            var seStr = SeString.Parse(payloadBytes);
            var terminatorIndex = seStr.Payloads.IndexOf(RawPayload.LinkTerminator);
            var payloads = terminatorIndex >= 0 ? seStr.Payloads.Take(terminatorIndex + 1).ToList() : seStr.Payloads;
            if (payloads.Count == 0) return;
            var linkPayload = payloads[0];
            if (linkPayload is DalamudLinkPayload link)
            {
                if (this.dalamudLinkHandlers.ContainsKey((link.Plugin, link.CommandId)))
                {
                    Log.Verbose($"Sending DalamudLink to {link.Plugin}: {link.CommandId}");
                    this.dalamudLinkHandlers[(link.Plugin, link.CommandId)].Invoke(link.CommandId, new SeString(payloads));
                }
                else
                {
                    Log.Debug($"No DalamudLink registered for {link.Plugin} with ID of {link.CommandId}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on InteractableLinkClicked hook");
        }
    }
}

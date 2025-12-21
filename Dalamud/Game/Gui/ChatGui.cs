using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using SeString = Dalamud.Game.Text.SeStringHandling.SeString;
using SeStringBuilder = Dalamud.Game.Text.SeStringHandling.SeStringBuilder;

namespace Dalamud.Game.Gui;

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ChatGui : IInternalDisposableService, IChatGui
{
    private static readonly ModuleLog Log = new("ChatGui");

    private readonly Queue<XivChatEntry> chatQueue = new();
    private readonly Dictionary<(string PluginName, uint CommandId), Action<uint, SeString>> dalamudLinkHandlers = new();

    private readonly Hook<PrintMessageDelegate> printMessageHook;
    private readonly Hook<InventoryItem.Delegates.Copy> inventoryItemCopyHook;
    private readonly Hook<LogViewer.Delegates.HandleLinkClick> handleLinkClickHook;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private ImmutableDictionary<(string PluginName, uint CommandId), Action<uint, SeString>>? dalamudLinkHandlersCopy;
    private uint dalamudChatHandlerId = 1000;

    [ServiceManager.ServiceConstructor]
    private ChatGui()
    {
        this.printMessageHook = Hook<PrintMessageDelegate>.FromAddress(RaptureLogModule.Addresses.PrintMessage.Value, this.HandlePrintMessageDetour);
        this.inventoryItemCopyHook = Hook<InventoryItem.Delegates.Copy>.FromAddress((nint)InventoryItem.StaticVirtualTablePointer->Copy, this.InventoryItemCopyDetour);
        this.handleLinkClickHook = Hook<LogViewer.Delegates.HandleLinkClick>.FromAddress(LogViewer.Addresses.HandleLinkClick.Value, this.HandleLinkClickDetour);

        this.printMessageHook.Enable();
        this.inventoryItemCopyHook.Enable();
        this.handleLinkClickHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate uint PrintMessageDelegate(RaptureLogModule* manager, XivChatType chatType, Utf8String* sender, Utf8String* message, int timestamp, byte silent);

    /// <inheritdoc/>
    public event IChatGui.OnMessageDelegate? ChatMessage;

    /// <inheritdoc/>
    public event IChatGui.OnCheckMessageHandledDelegate? CheckMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageHandledDelegate? ChatMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageUnhandledDelegate? ChatMessageUnhandled;

    /// <inheritdoc/>
    public uint LastLinkedItemId { get; private set; }

    /// <inheritdoc/>
    public byte LastLinkedItemFlags { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<(string PluginName, uint CommandId), Action<uint, SeString>> RegisteredLinkHandlers
    {
        get
        {
            var copy = this.dalamudLinkHandlersCopy;
            if (copy is not null)
                return copy;

            lock (this.dalamudLinkHandlers)
            {
                return this.dalamudLinkHandlersCopy ??=
                           this.dalamudLinkHandlers.ToImmutableDictionary(x => x.Key, x => x.Value);
            }
        }
    }

    /// <summary>
    /// Dispose of managed and unmanaged resources.
    /// </summary>
    void IInternalDisposableService.DisposeService()
    {
        this.printMessageHook.Dispose();
        this.inventoryItemCopyHook.Dispose();
        this.handleLinkClickHook.Dispose();
    }

    #region DalamudSeString

    /// <inheritdoc/>
    public void Print(XivChatEntry chat)
    {
        this.chatQueue.Enqueue(chat);
    }

    /// <inheritdoc/>
    public void Print(string message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, this.configuration.GeneralChatType, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void Print(SeString message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, this.configuration.GeneralChatType, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void PrintError(string message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, XivChatType.Urgent, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void PrintError(SeString message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, XivChatType.Urgent, messageTag, tagColor);
    }

    #endregion

    #region LuminaSeString

    /// <inheritdoc/>
    public void Print(ReadOnlySpan<byte> message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, this.configuration.GeneralChatType, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void PrintError(ReadOnlySpan<byte> message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, XivChatType.Urgent, messageTag, tagColor);
    }

    #endregion

    #region Chat Links

    /// <summary>
    /// Register a chat link handler.
    /// </summary>
    /// <remarks>Internal use only.</remarks>
    /// <param name="commandAction">The action to be executed.</param>
    /// <returns>Returns an SeString payload for the link.</returns>
    public DalamudLinkPayload AddChatLinkHandler(Action<uint, SeString> commandAction)
    {
        return this.AddChatLinkHandler("Dalamud", this.dalamudChatHandlerId++, commandAction);
    }

    /// <inheritdoc/>
    /// <remarks>Internal use only.</remarks>
    public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction)
    {
        return this.AddChatLinkHandler("Dalamud", commandId, commandAction);
    }

    /// <inheritdoc/>
    /// <remarks>Internal use only.</remarks>
    public void RemoveChatLinkHandler(uint commandId)
    {
        this.RemoveChatLinkHandler("Dalamud", commandId);
    }

    /// <inheritdoc/>
    /// <remarks>Internal use only.</remarks>
    public void RemoveChatLinkHandler()
    {
        this.RemoveChatLinkHandler("Dalamud");
    }

    #endregion

    /// <summary>
    /// Process a chat queue.
    /// </summary>
    public void UpdateQueue()
    {
        if (this.chatQueue.Count == 0)
            return;

        using var rssb = new RentedSeStringBuilder();
        Span<byte> namebuf = stackalloc byte[256];
        using var sender = new Utf8String();
        using var message = new Utf8String();
        while (this.chatQueue.TryDequeue(out var chat))
        {
            rssb.Builder.Clear();
            foreach (var c in UtfEnumerator.From(chat.MessageBytes, UtfEnumeratorFlags.Utf8SeString))
            {
                if (c.IsSeStringPayload)
                    rssb.Builder.Append((ReadOnlySeStringSpan)chat.MessageBytes.AsSpan(c.ByteOffset, c.ByteLength));
                else if (c.Value.IntValue == 0x202F)
                    rssb.Builder.BeginMacro(MacroCode.NonBreakingSpace).EndMacro();
                else
                    rssb.Builder.Append(c);
            }

            if (chat.NameBytes.Length + 1 < namebuf.Length)
            {
                chat.NameBytes.AsSpan().CopyTo(namebuf);
                namebuf[chat.NameBytes.Length] = 0;
                sender.SetString(namebuf);
            }
            else
            {
                sender.SetString(chat.NameBytes.NullTerminate());
            }

            message.SetString(rssb.Builder.GetViewAsSpan());

            var targetChannel = chat.Type ?? this.configuration.GeneralChatType;

            this.HandlePrintMessageDetour(
                RaptureLogModule.Instance(),
                targetChannel,
                &sender,
                &message,
                chat.Timestamp,
                (byte)(chat.Silent ? 1 : 0));
        }
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
        var payload = new DalamudLinkPayload { Plugin = pluginName, CommandId = commandId };
        lock (this.dalamudLinkHandlers)
        {
            this.dalamudLinkHandlers.Add((pluginName, commandId), commandAction);
            this.dalamudLinkHandlersCopy = null;
        }

        return payload;
    }

    /// <summary>
    /// Remove all handlers owned by a plugin.
    /// </summary>
    /// <param name="pluginName">The name of the plugin handling the links.</param>
    internal void RemoveChatLinkHandler(string pluginName)
    {
        lock (this.dalamudLinkHandlers)
        {
            var changed = false;

            foreach (var handler in this.RegisteredLinkHandlers.Keys.Where(k => k.PluginName == pluginName))
                changed |= this.dalamudLinkHandlers.Remove(handler);
            if (changed)
                this.dalamudLinkHandlersCopy = null;
        }
    }

    /// <summary>
    /// Remove a registered link handler.
    /// </summary>
    /// <param name="pluginName">The name of the plugin handling the link.</param>
    /// <param name="commandId">The ID of the command to be removed.</param>
    internal void RemoveChatLinkHandler(string pluginName, uint commandId)
    {
        lock (this.dalamudLinkHandlers)
        {
            if (this.dalamudLinkHandlers.Remove((pluginName, commandId)))
                this.dalamudLinkHandlersCopy = null;
        }
    }

    private void PrintTagged(SeString message, XivChatType channel, string? tag, ushort? color)
    {
        var builder = new SeStringBuilder();

        if (!tag.IsNullOrEmpty())
        {
            if (color is not null)
            {
                builder.AddUiForeground($"[{tag}] ", color.Value);
            }
            else
            {
                builder.AddText($"[{tag}] ");
            }
        }

        this.Print(new XivChatEntry
        {
            Message = builder.Build().Append(message),
            Type = channel,
        });
    }

    private void PrintTagged(ReadOnlySpan<byte> message, XivChatType channel, string? tag, ushort? color)
    {
        using var rssb = new RentedSeStringBuilder();

        if (!tag.IsNullOrEmpty())
        {
            if (color is not null)
            {
                rssb.Builder
                    .PushColorType(color.Value)
                    .Append($"[{tag}] ")
                    .PopColorType();
            }
            else
            {
                rssb.Builder.Append($"[{tag}] ");
            }
        }

        this.Print(new XivChatEntry
        {
            MessageBytes = rssb.Builder.Append((ReadOnlySeStringSpan)message).ToArray(),
            Type = channel,
        });
    }

    private void InventoryItemCopyDetour(InventoryItem* thisPtr, InventoryItem* otherPtr)
    {
        this.inventoryItemCopyHook.Original(thisPtr, otherPtr);

        try
        {
            this.LastLinkedItemId = otherPtr->ItemId;
            this.LastLinkedItemFlags = (byte)otherPtr->Flags;

            // Log.Verbose($"InventoryItemCopyDetour {thisPtr} {otherPtr} - linked:{this.LastLinkedItemId}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in InventoryItemCopyHook");
        }
    }

    private uint HandlePrintMessageDetour(RaptureLogModule* manager, XivChatType chatType, Utf8String* sender, Utf8String* message, int timestamp, byte silent)
    {
        var messageId = 0u;

        try
        {
            var parsedSender = SeString.Parse(sender->AsSpan());
            var parsedMessage = SeString.Parse(message->AsSpan());

            var terminatedSender = parsedSender.EncodeWithNullTerminator();
            var terminatedMessage = parsedMessage.EncodeWithNullTerminator();

            // Call events
            var isHandled = false;

            foreach (var action in Delegate.EnumerateInvocationList(this.CheckMessageHandled))
            {
                try
                {
                    action(chatType, timestamp, ref parsedSender, ref parsedMessage, ref isHandled);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not invoke registered OnCheckMessageHandledDelegate for {Name}", action.Method);
                }
            }

            if (!isHandled)
            {
                foreach (var action in Delegate.EnumerateInvocationList(this.ChatMessage))
                {
                    try
                    {
                        action(chatType, timestamp, ref parsedSender, ref parsedMessage, ref isHandled);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not invoke registered OnMessageDelegate for {Name}", action.Method);
                    }
                }
            }

            var possiblyModifiedSenderData = parsedSender.EncodeWithNullTerminator();
            var possiblyModifiedMessageData = parsedMessage.EncodeWithNullTerminator();

            if (!terminatedSender.SequenceEqual(possiblyModifiedSenderData))
            {
                Log.Verbose($"HandlePrintMessageDetour Sender modified: {new ReadOnlySeStringSpan(terminatedSender).ToMacroString()} -> {new ReadOnlySeStringSpan(possiblyModifiedSenderData).ToMacroString()}");
                sender->SetString(possiblyModifiedSenderData);
            }

            if (!terminatedMessage.SequenceEqual(possiblyModifiedMessageData))
            {
                Log.Verbose($"HandlePrintMessageDetour Message modified: {new ReadOnlySeStringSpan(terminatedMessage).ToMacroString()} -> {new ReadOnlySeStringSpan(possiblyModifiedMessageData).ToMacroString()}");
                message->SetString(possiblyModifiedMessageData);
            }

            // Print the original chat if it's handled.
            if (isHandled)
            {
                foreach (var d in Delegate.EnumerateInvocationList(this.ChatMessageHandled))
                    d(chatType, timestamp, parsedSender, parsedMessage);
            }
            else
            {
                messageId = this.printMessageHook.Original(manager, chatType, sender, message, timestamp, silent);
                foreach (var d in Delegate.EnumerateInvocationList(this.ChatMessageUnhandled))
                    d(chatType, timestamp, parsedSender, parsedMessage);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on OnChatMessage hook.");
            messageId = this.printMessageHook.Original(manager, chatType, sender, message, timestamp, silent);
        }

        return messageId;
    }

    private void HandleLinkClickDetour(LogViewer* thisPtr, LinkData* linkData)
    {
        if (linkData == null || linkData->Payload == null || (Payload.EmbeddedInfoType)(linkData->LinkType + 1) != Payload.EmbeddedInfoType.DalamudLink)
        {
            this.handleLinkClickHook.Original(thisPtr, linkData);
            return;
        }

        Log.Verbose($"InteractableLinkClicked: {Payload.EmbeddedInfoType.DalamudLink}");

        using var rssb = new RentedSeStringBuilder();

        try
        {
            var seStringSpan = new ReadOnlySeStringSpan(linkData->Payload);

            // read until link terminator
            foreach (var payload in seStringSpan)
            {
                rssb.Builder.Append(payload);

                if (payload.Type == ReadOnlySePayloadType.Macro &&
                    payload.MacroCode == MacroCode.Link &&
                    payload.TryGetExpression(out var expr1) &&
                    expr1.TryGetInt(out var expr1Val) &&
                    expr1Val == (int)LinkMacroPayloadType.Terminator)
                {
                    break;
                }
            }

            var seStr = SeString.Parse(rssb.Builder.ToArray());
            if (seStr.Payloads.Count == 0 || seStr.Payloads[0] is not DalamudLinkPayload link)
                return;

            if (this.RegisteredLinkHandlers.TryGetValue((link.Plugin, link.CommandId), out var value))
            {
                Log.Verbose($"Sending DalamudLink to {link.Plugin}: {link.CommandId}");
                value.Invoke(link.CommandId, seStr);
            }
            else
            {
                Log.Debug($"No DalamudLink registered for {link.Plugin} with ID of {link.CommandId}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in HandleLinkClickDetour");
        }
    }
}

/// <summary>
/// Plugin scoped version of ChatGui.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IChatGui>]
#pragma warning restore SA1015
internal class ChatGuiPluginScoped : IInternalDisposableService, IChatGui
{
    [ServiceManager.ServiceDependency]
    private readonly ChatGui chatGuiService = Service<ChatGui>.Get();

    private readonly LocalPlugin plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatGuiPluginScoped"/> class.
    /// </summary>
    /// <param name="plugin">The plugin.</param>
    internal ChatGuiPluginScoped(LocalPlugin plugin)
    {
        this.plugin = plugin;
        this.chatGuiService.ChatMessage += this.OnMessageForward;
        this.chatGuiService.CheckMessageHandled += this.OnCheckMessageForward;
        this.chatGuiService.ChatMessageHandled += this.OnMessageHandledForward;
        this.chatGuiService.ChatMessageUnhandled += this.OnMessageUnhandledForward;
    }

    /// <inheritdoc/>
    public event IChatGui.OnMessageDelegate? ChatMessage;

    /// <inheritdoc/>
    public event IChatGui.OnCheckMessageHandledDelegate? CheckMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageHandledDelegate? ChatMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageUnhandledDelegate? ChatMessageUnhandled;

    /// <inheritdoc/>
    public uint LastLinkedItemId => this.chatGuiService.LastLinkedItemId;

    /// <inheritdoc/>
    public byte LastLinkedItemFlags => this.chatGuiService.LastLinkedItemFlags;

    /// <inheritdoc/>
    public IReadOnlyDictionary<(string PluginName, uint CommandId), Action<uint, SeString>> RegisteredLinkHandlers => this.chatGuiService.RegisteredLinkHandlers;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.chatGuiService.ChatMessage -= this.OnMessageForward;
        this.chatGuiService.CheckMessageHandled -= this.OnCheckMessageForward;
        this.chatGuiService.ChatMessageHandled -= this.OnMessageHandledForward;
        this.chatGuiService.ChatMessageUnhandled -= this.OnMessageUnhandledForward;

        this.ChatMessage = null;
        this.CheckMessageHandled = null;
        this.ChatMessageHandled = null;
        this.ChatMessageUnhandled = null;
    }

    /// <inheritdoc/>
    public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<uint, SeString> commandAction)
        => this.chatGuiService.AddChatLinkHandler(this.plugin.InternalName, commandId, commandAction);

    /// <inheritdoc/>
    public void RemoveChatLinkHandler(uint commandId)
        => this.chatGuiService.RemoveChatLinkHandler(this.plugin.InternalName, commandId);

    /// <inheritdoc/>
    public void RemoveChatLinkHandler()
        => this.chatGuiService.RemoveChatLinkHandler(this.plugin.InternalName);

    /// <inheritdoc/>
    public void Print(XivChatEntry chat)
        => this.chatGuiService.Print(chat);

    /// <inheritdoc/>
    public void Print(string message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.Print(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void Print(SeString message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.Print(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void PrintError(string message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.PrintError(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void PrintError(SeString message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.PrintError(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void Print(ReadOnlySpan<byte> message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.Print(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void PrintError(ReadOnlySpan<byte> message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.PrintError(message, messageTag, tagColor);

    private void OnMessageForward(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        => this.ChatMessage?.Invoke(type, timestamp, ref sender, ref message, ref isHandled);

    private void OnCheckMessageForward(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        => this.CheckMessageHandled?.Invoke(type, timestamp, ref sender, ref message, ref isHandled);

    private void OnMessageHandledForward(XivChatType type, int timestamp, SeString sender, SeString message)
        => this.ChatMessageHandled?.Invoke(type, timestamp, sender, message);

    private void OnMessageUnhandledForward(XivChatType type, int timestamp, SeString sender, SeString message)
        => this.ChatMessageUnhandled?.Invoke(type, timestamp, sender, message);
}

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
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

namespace Dalamud.Game.Gui;

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ChatGui : IInternalDisposableService, IChatGui
{
    private static readonly ModuleLog Log = ModuleLog.Create<ChatGui>();

    private readonly Queue<XivChatEntry> chatQueue = new();
    private readonly Dictionary<(string PluginName, uint CommandId), Action<DalamudLinkPayload>> dalamudLinkHandlers = [];
    private readonly List<nint> seenLogMessageObjects = [];
    private readonly ChatMessage currentChatMessage = new();

    private readonly Hook<RaptureLogModule.Delegates.PrintMessage> printMessageHook;
    private readonly Hook<InventoryItem.Delegates.Copy> inventoryItemCopyHook;
    private readonly Hook<LogViewer.Delegates.HandleLinkClick> handleLinkClickHook;
    private readonly Hook<RaptureLogModule.Delegates.Update> handleLogModuleUpdate;

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private ImmutableDictionary<(string PluginName, uint CommandId), Action<DalamudLinkPayload>>? dalamudLinkHandlersCopy;
    private uint dalamudChatHandlerId = 1000;

    [ServiceManager.ServiceConstructor]
    private ChatGui()
    {
        this.printMessageHook = Hook<RaptureLogModule.Delegates.PrintMessage>.FromAddress(RaptureLogModule.Addresses.PrintMessage.Value, this.HandlePrintMessageDetour);
        this.inventoryItemCopyHook = Hook<InventoryItem.Delegates.Copy>.FromAddress((nint)InventoryItem.StaticVirtualTablePointer->Copy, this.InventoryItemCopyDetour);
        this.handleLinkClickHook = Hook<LogViewer.Delegates.HandleLinkClick>.FromAddress(LogViewer.Addresses.HandleLinkClick.Value, this.HandleLinkClickDetour);
        this.handleLogModuleUpdate = Hook<RaptureLogModule.Delegates.Update>.FromAddress(RaptureLogModule.Addresses.Update.Value, this.UpdateDetour);

        this.printMessageHook.Enable();
        this.inventoryItemCopyHook.Enable();
        this.handleLinkClickHook.Enable();
        this.handleLogModuleUpdate.Enable();

        this.framework.BeforeUpdate += this.UpdateQueue;
    }

    /// <inheritdoc/>
    public event IChatGui.OnHandleableChatMessageDelegate? ChatMessage;

    /// <inheritdoc/>
    public event IChatGui.OnHandleableChatMessageDelegate? CheckMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnChatMessageDelegate? ChatMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnChatMessageDelegate? ChatMessageUnhandled;

    /// <inheritdoc/>
    public event IChatGui.OnLogMessageDelegate? LogMessage;

    /// <inheritdoc/>
    public uint LastLinkedItemId { get; private set; }

    /// <inheritdoc/>
    public byte LastLinkedItemFlags { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<(string PluginName, uint CommandId), Action<DalamudLinkPayload>> RegisteredLinkHandlers
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
        this.framework.BeforeUpdate -= this.UpdateQueue;

        this.printMessageHook.Dispose();
        this.inventoryItemCopyHook.Dispose();
        this.handleLinkClickHook.Dispose();
        this.handleLogModuleUpdate.Dispose();
    }

    /// <inheritdoc/>
    public void Print(XivChatEntry chat)
    {
        this.chatQueue.Enqueue(chat);
    }

    /// <inheritdoc/>
    public void Print(ReadOnlySeString message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message.AsSpan(), this.configuration.GeneralChatType, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void Print(ReadOnlySeStringSpan message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, this.configuration.GeneralChatType, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void PrintError(ReadOnlySeString message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message.AsSpan(), XivChatType.Urgent, messageTag, tagColor);
    }

    /// <inheritdoc/>
    public void PrintError(ReadOnlySeStringSpan message, string? messageTag = null, ushort? tagColor = null)
    {
        this.PrintTagged(message, XivChatType.Urgent, messageTag, tagColor);
    }

    #region Chat Links

    /// <summary>
    /// Register a chat link handler.
    /// </summary>
    /// <remarks>Internal use only.</remarks>
    /// <param name="commandAction">The action to be executed.</param>
    /// <returns>Returns an SeString payload for the link.</returns>
    public DalamudLinkPayload AddChatLinkHandler(Action<DalamudLinkPayload> commandAction)
    {
        return this.AddChatLinkHandler("Dalamud", this.dalamudChatHandlerId++, commandAction);
    }

    /// <inheritdoc/>
    /// <remarks>Internal use only.</remarks>
    public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<DalamudLinkPayload> commandAction)
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
    /// Create a link handler.
    /// </summary>
    /// <param name="pluginName">The name of the plugin handling the link.</param>
    /// <param name="commandId">The ID of the command to run.</param>
    /// <param name="commandAction">The command action itself.</param>
    /// <returns>A payload for handling.</returns>
    internal DalamudLinkPayload AddChatLinkHandler(string pluginName, uint commandId, Action<DalamudLinkPayload> commandAction)
    {
        var payload = new DalamudLinkPayload(commandId, pluginName);

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

    /// <summary>
    /// Process a chat queue.
    /// </summary>
    /// <param name="framework">The Framework instance.</param>
    private void UpdateQueue(IFramework framework)
    {
        if (this.chatQueue.Count == 0)
            return;

        using var rssb = new RentedSeStringBuilder();
        using var sender = new Utf8String();
        using var message = new Utf8String();

        while (this.chatQueue.TryDequeue(out var chat))
        {
            // set sender
            sender.SetString(rssb.Builder
                .Clear()
                .Append(chat.Name)
                .GetViewAsSpan());

            // set message
            rssb.Builder.Clear();

            foreach (var c in Lumina.Text.UtfEnumerator.From(chat.Message, Lumina.Text.UtfEnumeratorFlags.Utf8SeString))
            {
                if (c.IsSeStringPayload)
                    rssb.Builder.Append((ReadOnlySeStringSpan)chat.Message.Data.Span[c.ByteOffset..(c.ByteOffset + c.ByteLength)]);
                else if (c.Value.IntValue == 0x202F)
                    rssb.Builder.BeginMacro(MacroCode.NonBreakingSpace).EndMacro();
                else
                    rssb.Builder.Append(c);
            }

            message.SetString(rssb.Builder.GetViewAsSpan());

            var targetChannel = chat.Type ?? this.configuration.GeneralChatType;

            this.HandlePrintMessageDetour(
                RaptureLogModule.Instance(),
                (ushort)targetChannel,
                &sender,
                &message,
                chat.Timestamp,
                chat.Silent);
        }
    }

    private void PrintTagged(ReadOnlySeStringSpan message, XivChatType channel, string? tag, ushort? color)
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
            Message = rssb.Builder.Append(message).ToReadOnlySeString(),
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

    private uint HandlePrintMessageDetour(RaptureLogModule* manager, ushort logInfo, Utf8String* sender, Utf8String* message, int timestamp, bool silent)
    {
        var messageId = 0u;

        try
        {
            var logKind = (XivChatType)(logInfo & 0x7F);
            var sourceKind = (XivChatRelationKind)((logInfo >> 11) & 0xF);
            var targetKind = (XivChatRelationKind)((logInfo >> 7) & 0xF);

            var roSender = sender->AsReadOnlySeString();
            var roMessage = message->AsReadOnlySeString();

            this.currentChatMessage.SetData(logKind, sourceKind, targetKind, roSender, roMessage, timestamp);

            // First pass
            foreach (var action in Delegate.EnumerateInvocationList(this.ChatMessage))
            {
                try
                {
                    action(this.currentChatMessage);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not invoke registered OnHandleableChatMessageDelegate for {Name}", action.Method);
                }
            }

            // Second pass
            if (!this.currentChatMessage.IsHandled)
            {
                foreach (var action in Delegate.EnumerateInvocationList(this.CheckMessageHandled))
                {
                    try
                    {
                        action(this.currentChatMessage);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not invoke registered OnHandleableChatMessageDelegate for {Name}", action.Method);
                    }
                }
            }

            // Check for modifications
            if (this.currentChatMessage.SenderModified)
            {
                Log.Verbose($"HandlePrintMessageDetour Sender modified: {sender->AsReadOnlySeStringSpan().ToMacroString()} -> {this.currentChatMessage.Sender.ToMacroString()}");
                using var rssb = new RentedSeStringBuilder();
                sender->SetString(rssb.Builder.Append(this.currentChatMessage.Sender).GetViewAsSpan());
            }

            if (this.currentChatMessage.MessageModified)
            {
                Log.Verbose($"HandlePrintMessageDetour Message modified: {message->AsReadOnlySeStringSpan().ToMacroString()} -> {this.currentChatMessage.Message.ToMacroString()}");
                using var rssb = new RentedSeStringBuilder();
                message->SetString(rssb.Builder.Append(this.currentChatMessage.Message).GetViewAsSpan());
            }

            // If not handled by a plugin, let the game handle it (prints it to chat)
            if (!this.currentChatMessage.IsHandled)
            {
                messageId = this.printMessageHook.Original(manager, logInfo, sender, message, timestamp, silent);
            }

            // Third pass
            var callback = this.currentChatMessage.IsHandled
                ? this.ChatMessageHandled
                : this.ChatMessageUnhandled;

            foreach (var action in Delegate.EnumerateInvocationList(callback))
            {
                try
                {
                    action(this.currentChatMessage);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not invoke registered OnChatMessageDelegate for {Name}", action.Method);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on OnChatMessage hook.");
            messageId = this.printMessageHook.Original(manager, logInfo, sender, message, timestamp, silent);
        }

        return messageId;
    }

    private void HandleLinkClickDetour(LogViewer* thisPtr, LinkData* linkData)
    {
        if (linkData == null || linkData->Payload == null || (LinkMacroPayloadType)(linkData->LinkType + 1) != DalamudLinkPayload.LinkType)
        {
            this.handleLinkClickHook.Original(thisPtr, linkData);
            return;
        }

        Log.Verbose("InteractableLinkClicked: DalamudLink");

        try
        {
            var seStringSpan = new ReadOnlySeStringSpan(linkData->Payload);

            foreach (var payload in seStringSpan)
            {
                // read DalamudLink
                if (payload.TryParseDalamudLink(out var dalamudLinkPayload))
                {
                    if (this.RegisteredLinkHandlers.TryGetValue((dalamudLinkPayload.PluginName, dalamudLinkPayload.CommandId), out var value))
                    {
                        Log.Verbose($"Sending DalamudLink to {dalamudLinkPayload.PluginName}: {dalamudLinkPayload.CommandId}");
                        value.Invoke(dalamudLinkPayload);
                    }
                    else
                    {
                        Log.Debug($"No DalamudLink registered for {dalamudLinkPayload.PluginName} with ID of {dalamudLinkPayload.CommandId}");
                    }

                    break;
                }

                // read until link terminator as fallback
                if (payload.IsLink(LinkMacroPayloadType.Terminator))
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in HandleLinkClickDetour");
        }
    }

    private void UpdateDetour(RaptureLogModule* thisPtr)
    {
        try
        {
            var sharedLogMessage = Chat.LogMessage.Instance;
            var sharedParams = Chat.LogMessageParameterList.Instance;

            foreach (ref var item in thisPtr->LogMessageQueue)
            {
                var address = (LogMessageQueueItem*)Unsafe.AsPointer(ref item);

                // skip any entries that survived the previous Update call as the event was already called for them
                if (this.seenLogMessageObjects.Contains((nint)address))
                    continue;

                sharedLogMessage.Pointer = address;
                sharedParams.Pointer = &address->Parameters;

                foreach (var action in Delegate.EnumerateInvocationList(this.LogMessage))
                {
                    try
                    {
                        action(sharedLogMessage);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not invoke registered OnLogMessageDelegate for {Name}", action.Method);
                    }
                }

                if (sharedLogMessage.IsHandled)
                {
                    sharedLogMessage.IsHandled = false;
                    // LogMessage 0 is an empty string that is displayed nowhere
                    // setting a non-existent row would "properly" skip the entry,
                    // but the game attempts to read that row for 150 frames before
                    // continuing with the next item in the queue
                    item.LogMessageId = 0;
                }
            }

            sharedLogMessage.Pointer = null;
            sharedParams.Pointer = null;

            this.handleLogModuleUpdate.Original(thisPtr);

            // record the log messages for that we already called the event, but are still in the queue
            this.seenLogMessageObjects.Clear();
            foreach (ref var item in thisPtr->LogMessageQueue)
            {
                this.seenLogMessageObjects.Add((nint)Unsafe.AsPointer(ref item));
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in UpdateDetour");
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
        this.chatGuiService.ChatMessageHandled += this.OnMessageHandledForward;
        this.chatGuiService.ChatMessageUnhandled += this.OnMessageUnhandledForward;
        this.chatGuiService.LogMessage += this.OnLogMessageForward;
    }

    /// <inheritdoc/>
    public event IChatGui.OnHandleableChatMessageDelegate? ChatMessage;

    /// <inheritdoc/>
    public event IChatGui.OnHandleableChatMessageDelegate? CheckMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnChatMessageDelegate? ChatMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnChatMessageDelegate? ChatMessageUnhandled;

    /// <inheritdoc/>
    public event IChatGui.OnLogMessageDelegate? LogMessage;

    /// <inheritdoc/>
    public uint LastLinkedItemId => this.chatGuiService.LastLinkedItemId;

    /// <inheritdoc/>
    public byte LastLinkedItemFlags => this.chatGuiService.LastLinkedItemFlags;

    /// <inheritdoc/>
    public IReadOnlyDictionary<(string PluginName, uint CommandId), Action<DalamudLinkPayload>> RegisteredLinkHandlers => this.chatGuiService.RegisteredLinkHandlers;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.chatGuiService.ChatMessage -= this.OnMessageForward;
        this.chatGuiService.CheckMessageHandled -= this.OnCheckMessageHandledForward;
        this.chatGuiService.ChatMessageHandled -= this.OnMessageHandledForward;
        this.chatGuiService.ChatMessageUnhandled -= this.OnMessageUnhandledForward;
        this.chatGuiService.LogMessage -= this.OnLogMessageForward;

        this.ChatMessage = null;
        this.ChatMessageHandled = null;
        this.ChatMessageUnhandled = null;
        this.LogMessage = null;
    }

    /// <inheritdoc/>
    public DalamudLinkPayload AddChatLinkHandler(uint commandId, Action<DalamudLinkPayload> commandAction)
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
    public void Print(ReadOnlySeString message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.Print(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void Print(ReadOnlySeStringSpan message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.Print(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void PrintError(ReadOnlySeString message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.PrintError(message, messageTag, tagColor);

    /// <inheritdoc/>
    public void PrintError(ReadOnlySeStringSpan message, string? messageTag = null, ushort? tagColor = null)
        => this.chatGuiService.PrintError(message, messageTag, tagColor);

    private void OnMessageForward(IHandleableChatMessage message)
        => this.ChatMessage?.Invoke(message);

    private void OnCheckMessageHandledForward(IHandleableChatMessage message)
        => this.CheckMessageHandled?.Invoke(message);

    private void OnMessageHandledForward(IChatMessage message)
        => this.ChatMessageHandled?.Invoke(message);

    private void OnMessageUnhandledForward(IChatMessage message)
        => this.ChatMessageUnhandled?.Invoke(message);

    private void OnLogMessageForward(Chat.ILogMessage message)
        => this.LogMessage?.Invoke(message);
}

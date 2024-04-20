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
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace Dalamud.Game.Gui;

// TODO(api10): Update IChatGui, ChatGui and XivChatEntry to use correct types and names:
//  "uint SenderId" should be "int Timestamp".
//  "IntPtr Parameters" should be something like "bool Silent". It suppresses new message sounds in certain channels.
//    This has to be a 1 byte boolean, so only change it to bool if marshalling is disabled.

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal sealed unsafe class ChatGui : IInternalDisposableService, IChatGui
{
    private static readonly ModuleLog Log = new("ChatGui");

    private readonly ChatGuiAddressResolver address;

    private readonly Queue<XivChatEntry> chatQueue = new();
    private readonly Dictionary<(string PluginName, uint CommandId), Action<uint, SeString>> dalamudLinkHandlers = new();

    private readonly Hook<PrintMessageDelegate> printMessageHook;
    private readonly Hook<PopulateItemLinkDelegate> populateItemLinkHook;
    private readonly Hook<InteractableLinkClickedDelegate> interactableLinkClickedHook;

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private ImmutableDictionary<(string PluginName, uint CommandId), Action<uint, SeString>>? dalamudLinkHandlersCopy;

    [ServiceManager.ServiceConstructor]
    private ChatGui(TargetSigScanner sigScanner)
    {
        this.address = new ChatGuiAddressResolver();
        this.address.Setup(sigScanner);

        this.printMessageHook = Hook<PrintMessageDelegate>.FromAddress((nint)RaptureLogModule.Addresses.PrintMessage.Value, this.HandlePrintMessageDetour);
        this.populateItemLinkHook = Hook<PopulateItemLinkDelegate>.FromAddress(this.address.PopulateItemLinkObject, this.HandlePopulateItemLinkDetour);
        this.interactableLinkClickedHook = Hook<InteractableLinkClickedDelegate>.FromAddress(this.address.InteractableLinkClicked, this.InteractableLinkClickedDetour);

        this.printMessageHook.Enable();
        this.populateItemLinkHook.Enable();
        this.interactableLinkClickedHook.Enable();
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate uint PrintMessageDelegate(RaptureLogModule* manager, XivChatType chatType, Utf8String* sender, Utf8String* message, int timestamp, byte silent);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void PopulateItemLinkDelegate(IntPtr linkObjectPtr, IntPtr itemInfoPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void InteractableLinkClickedDelegate(IntPtr managerPtr, IntPtr messagePtr);

    /// <inheritdoc/>
    public event IChatGui.OnMessageDelegate? ChatMessage;

    /// <inheritdoc/>
    public event IChatGui.OnCheckMessageHandledDelegate? CheckMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageHandledDelegate? ChatMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageUnhandledDelegate? ChatMessageUnhandled;

    /// <inheritdoc/>
    public int LastLinkedItemId { get; private set; }

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
        this.populateItemLinkHook.Dispose();
        this.interactableLinkClickedHook.Dispose();
    }

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

    /// <summary>
    /// Process a chat queue.
    /// </summary>
    public void UpdateQueue()
    {
        while (this.chatQueue.Count > 0)
        {
            var chat = this.chatQueue.Dequeue();
            var replacedMessage = new SeStringBuilder();

            // Normalize Unicode NBSP to the built-in one, as the former won't renderl
            foreach (var payload in chat.Message.Payloads)
            {
                if (payload is TextPayload { Text: not null } textPayload)
                {
                    var split = textPayload.Text.Split("\u202f"); // NARROW NO-BREAK SPACE
                    for (var i = 0; i < split.Length; i++)
                    {
                        replacedMessage.AddText(split[i]);
                        if (i + 1 < split.Length)
                            replacedMessage.Add(new RawPayload([0x02, (byte)Lumina.Text.Payloads.PayloadType.Indent, 0x01, 0x03]));
                    }
                }
                else
                {
                    replacedMessage.Add(payload);
                }
            }

            var sender = Utf8String.FromSequence(chat.Name.Encode());
            var message = Utf8String.FromSequence(replacedMessage.BuiltString.Encode());

            this.HandlePrintMessageDetour(RaptureLogModule.Instance(), chat.Type, sender, message, (int)chat.SenderId, (byte)(chat.Parameters != 0 ? 1 : 0));

            sender->Dtor(true);
            message->Dtor(true);
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

    private void PrintTagged(string message, XivChatType channel, string? tag, ushort? color)
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
            Message = builder.AddText(message).Build(),
            Type = channel,
        });
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

    private uint HandlePrintMessageDetour(RaptureLogModule* manager, XivChatType chatType, Utf8String* sender, Utf8String* message, int timestamp, byte silent)
    {
        var messageId = 0u;

        try
        {
            var originalSenderData = sender->AsSpan().ToArray();
            var originalMessageData = message->AsSpan().ToArray();

            var parsedSender = SeString.Parse(originalSenderData);
            var parsedMessage = SeString.Parse(originalMessageData);

            // Call events
            var isHandled = false;

            var invocationList = this.CheckMessageHandled!.GetInvocationList();
            foreach (var @delegate in invocationList)
            {
                try
                {
                    var messageHandledDelegate = @delegate as IChatGui.OnCheckMessageHandledDelegate;
                    messageHandledDelegate!.Invoke(chatType, (uint)timestamp, ref parsedSender, ref parsedMessage, ref isHandled);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Could not invoke registered OnCheckMessageHandledDelegate for {Name}", @delegate.Method.Name);
                }
            }

            if (!isHandled)
            {
                invocationList = this.ChatMessage!.GetInvocationList();
                foreach (var @delegate in invocationList)
                {
                    try
                    {
                        var messageHandledDelegate = @delegate as IChatGui.OnMessageDelegate;
                        messageHandledDelegate!.Invoke(chatType, (uint)timestamp, ref parsedSender, ref parsedMessage, ref isHandled);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Could not invoke registered OnMessageDelegate for {Name}", @delegate.Method.Name);
                    }
                }
            }

            var possiblyModifiedSenderData = parsedSender.Encode();
            var possiblyModifiedMessageData = parsedMessage.Encode();

            if (!Util.FastByteArrayCompare(originalSenderData, possiblyModifiedSenderData))
            {
                Log.Verbose($"HandlePrintMessageDetour Sender modified: {SeString.Parse(originalSenderData)} -> {parsedSender}");
                sender->SetString(possiblyModifiedSenderData);
            }

            if (!Util.FastByteArrayCompare(originalMessageData, possiblyModifiedMessageData))
            {
                Log.Verbose($"HandlePrintMessageDetour Message modified: {SeString.Parse(originalMessageData)} -> {parsedMessage}");
                message->SetString(possiblyModifiedMessageData);
            }

            // Print the original chat if it's handled.
            if (isHandled)
            {
                this.ChatMessageHandled?.Invoke(chatType, (uint)timestamp, parsedSender, parsedMessage);
            }
            else
            {
                messageId = this.printMessageHook.Original(manager, chatType, sender, message, timestamp, silent);
                this.ChatMessageUnhandled?.Invoke(chatType, (uint)timestamp, parsedSender, parsedMessage);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on OnChatMessage hook.");
            messageId = this.printMessageHook.Original(manager, chatType, sender, message, timestamp, silent);
        }

        return messageId;
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
            var seStr = MemoryHelper.ReadSeStringNullTerminated(payloadPtr);
            var terminatorIndex = seStr.Payloads.IndexOf(RawPayload.LinkTerminator);
            var payloads = terminatorIndex >= 0 ? seStr.Payloads.Take(terminatorIndex + 1).ToList() : seStr.Payloads;
            if (payloads.Count == 0) return;
            var linkPayload = payloads[0];
            if (linkPayload is DalamudLinkPayload link)
            {
                if (this.RegisteredLinkHandlers.TryGetValue((link.Plugin, link.CommandId), out var value))
                {
                    Log.Verbose($"Sending DalamudLink to {link.Plugin}: {link.CommandId}");
                    value.Invoke(link.CommandId, new SeString(payloads));
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

/// <summary>
/// Plugin scoped version of ChatGui.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<IChatGui>]
#pragma warning restore SA1015
internal class ChatGuiPluginScoped : IInternalDisposableService, IChatGui
{
    [ServiceManager.ServiceDependency]
    private readonly ChatGui chatGuiService = Service<ChatGui>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatGuiPluginScoped"/> class.
    /// </summary>
    internal ChatGuiPluginScoped()
    {
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
    public int LastLinkedItemId => this.chatGuiService.LastLinkedItemId;

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

    private void OnMessageForward(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        => this.ChatMessage?.Invoke(type, senderId, ref sender, ref message, ref isHandled);

    private void OnCheckMessageForward(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        => this.CheckMessageHandled?.Invoke(type, senderId, ref sender, ref message, ref isHandled);

    private void OnMessageHandledForward(XivChatType type, uint senderId, SeString sender, SeString message)
        => this.ChatMessageHandled?.Invoke(type, senderId, sender, message);

    private void OnMessageUnhandledForward(XivChatType type, uint senderId, SeString sender, SeString message)
        => this.ChatMessageUnhandled?.Invoke(type, senderId, sender, message);
}

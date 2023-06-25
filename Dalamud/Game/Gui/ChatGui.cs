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
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Serilog;

namespace Dalamud.Game.Gui;

/// <summary>
/// This class handles interacting with the native chat UI.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IChatGui>]
#pragma warning restore SA1015
public sealed class ChatGui : IDisposable, IServiceType, IChatGui
{
    private readonly ChatGuiAddressResolver address;

    private readonly Queue<XivChatEntry> chatQueue = new();
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
    private ChatGui(SigScanner sigScanner)
    {
        this.address = new ChatGuiAddressResolver();
        this.address.Setup(sigScanner);

        this.printMessageHook = Hook<PrintMessageDelegate>.FromAddress(this.address.PrintMessage, this.HandlePrintMessageDetour);
        this.populateItemLinkHook = Hook<PopulateItemLinkDelegate>.FromAddress(this.address.PopulateItemLinkObject, this.HandlePopulateItemLinkDetour);
        this.interactableLinkClickedHook = Hook<InteractableLinkClickedDelegate>.FromAddress(this.address.InteractableLinkClicked, this.InteractableLinkClickedDetour);
    }
    
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr PrintMessageDelegate(IntPtr manager, XivChatType chatType, IntPtr senderName, IntPtr message, uint senderId, IntPtr parameter);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void PopulateItemLinkDelegate(IntPtr linkObjectPtr, IntPtr itemInfoPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void InteractableLinkClickedDelegate(IntPtr managerPtr, IntPtr messagePtr);

    /// <inheritdoc/>
    public event IChatGui.OnMessageDelegate ChatMessage;

    /// <inheritdoc/>
    public event IChatGui.OnCheckMessageHandledDelegate CheckMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageHandledDelegate ChatMessageHandled;

    /// <inheritdoc/>
    public event IChatGui.OnMessageUnhandledDelegate ChatMessageUnhandled;

    /// <inheritdoc/>
    public int LastLinkedItemId { get; private set; }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void PrintChat(XivChatEntry chat)
    {
        this.chatQueue.Enqueue(chat);
    }

    /// <inheritdoc/>
    public void Print(string message)
    {
        // Log.Verbose("[CHATGUI PRINT REGULAR]{0}", message);
        this.PrintChat(new XivChatEntry
        {
            Message = message,
            Type = this.configuration.GeneralChatType,
        });
    }

    /// <inheritdoc/>
    public void Print(SeString message)
    {
        // Log.Verbose("[CHATGUI PRINT SESTRING]{0}", message.TextValue);
        this.PrintChat(new XivChatEntry
        {
            Message = message,
            Type = this.configuration.GeneralChatType,
        });
    }

    /// <inheritdoc/>
    public void PrintError(string message)
    {
        // Log.Verbose("[CHATGUI PRINT REGULAR ERROR]{0}", message);
        this.PrintChat(new XivChatEntry
        {
            Message = message,
            Type = XivChatType.Urgent,
        });
    }

    /// <inheritdoc/>
    public void PrintError(SeString message)
    {
        // Log.Verbose("[CHATGUI PRINT SESTRING ERROR]{0}", message.TextValue);
        this.PrintChat(new XivChatEntry
        {
            Message = message,
            Type = XivChatType.Urgent,
        });
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

            this.HandlePrintMessageDetour(this.baseAddress, chat.Type, senderOwned.Address, messageOwned.Address, chat.SenderId, chat.Parameters);
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

    private IntPtr HandlePrintMessageDetour(IntPtr manager, XivChatType chattype, IntPtr pSenderName, IntPtr pMessage, uint senderid, IntPtr parameter)
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

            // Log.Debug($"HandlePrintMessageDetour {manager} - [{chattype}] [{BitConverter.ToString(message.RawData).Replace("-", " ")}] {message.Value} from {senderName.Value}");

            // Call events
            var isHandled = false;

            var invocationList = this.CheckMessageHandled.GetInvocationList();
            foreach (var @delegate in invocationList)
            {
                try
                {
                    var messageHandledDelegate = @delegate as IChatGui.OnCheckMessageHandledDelegate;
                    messageHandledDelegate!.Invoke(chattype, senderid, ref parsedSender, ref parsedMessage, ref isHandled);
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
                        var messageHandledDelegate = @delegate as IChatGui.OnMessageDelegate;
                        messageHandledDelegate!.Invoke(chattype, senderid, ref parsedSender, ref parsedMessage, ref isHandled);
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
                Log.Debug($"HandlePrintMessageDetour String modified: {originalMessageData}({messagePtr}) -> {message}({allocatedString.Address})");
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
                    $"HandlePrintMessageDetour Sender modified: {originalSenderData}({senderPtr}) -> {sender}({allocatedStringSender.Address})");
                senderPtr = allocatedStringSender.Address;
            }

            // Print the original chat if it's handled.
            if (isHandled)
            {
                this.ChatMessageHandled?.Invoke(chattype, senderid, parsedSender, parsedMessage);
            }
            else
            {
                retVal = this.printMessageHook.Original(manager, chattype, senderPtr, messagePtr, senderid, parameter);
                this.ChatMessageUnhandled?.Invoke(chattype, senderid, parsedSender, parsedMessage);
            }

            if (this.baseAddress == IntPtr.Zero)
                this.baseAddress = manager;

            allocatedString?.Dispose();
            allocatedStringSender?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception on OnChatMessage hook.");
            retVal = this.printMessageHook.Original(manager, chattype, pSenderName, pMessage, senderid, parameter);
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

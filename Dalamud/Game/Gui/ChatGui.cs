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
using Serilog;

namespace Dalamud.Game.Gui
{
    /// <summary>
    /// This class handles interacting with the native chat UI.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class ChatGui : IDisposable
    {
        private readonly ChatGuiAddressResolver address;

        private readonly Queue<XivChatEntry> chatQueue = new();
        private readonly Dictionary<(string PluginName, uint CommandId), Action<uint, SeString>> dalamudLinkHandlers = new();

        private readonly Hook<PrintMessageDelegate> printMessageHook;
        private readonly Hook<PopulateItemLinkDelegate> populateItemLinkHook;
        private readonly Hook<InteractableLinkClickedDelegate> interactableLinkClickedHook;

        private IntPtr baseAddress = IntPtr.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatGui"/> class.
        /// </summary>
        /// <param name="baseAddress">The base address of the ChatManager.</param>
        internal ChatGui(IntPtr baseAddress)
        {
            this.address = new ChatGuiAddressResolver(baseAddress);
            this.address.Setup();

            Log.Verbose($"Chat manager address 0x{this.address.BaseAddress.ToInt64():X}");

            this.printMessageHook = new Hook<PrintMessageDelegate>(this.address.PrintMessage, this.HandlePrintMessageDetour);
            this.populateItemLinkHook = new Hook<PopulateItemLinkDelegate>(this.address.PopulateItemLinkObject, this.HandlePopulateItemLinkDetour);
            this.interactableLinkClickedHook = new Hook<InteractableLinkClickedDelegate>(this.address.InteractableLinkClicked, this.InteractableLinkClickedDetour);
        }

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

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PrintMessageDelegate(IntPtr manager, XivChatType chatType, IntPtr senderName, IntPtr message, uint senderId, IntPtr parameter);

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
        /// Enables this module.
        /// </summary>
        public void Enable()
        {
            this.printMessageHook.Enable();
            this.populateItemLinkHook.Enable();
            this.interactableLinkClickedHook.Enable();
        }

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
        /// Queue a chat message. While method is named as PrintChat, it only add a entry to the queue,
        /// later to be processed when UpdateQueue() is called.
        /// </summary>
        /// <param name="chat">A message to send.</param>
        public void PrintChat(XivChatEntry chat)
        {
            this.chatQueue.Enqueue(chat);
        }

        /// <summary>
        /// Queue a chat message. While method is named as PrintChat (it calls it internally), it only add a entry to the queue,
        /// later to be processed when UpdateQueue() is called.
        /// </summary>
        /// <param name="message">A message to send.</param>
        public void Print(string message)
        {
            var configuration = Service<DalamudConfiguration>.Get();

            // Log.Verbose("[CHATGUI PRINT REGULAR]{0}", message);
            this.PrintChat(new XivChatEntry
            {
                Message = message,
                Type = configuration.GeneralChatType,
            });
        }

        /// <summary>
        /// Queue a chat message. While method is named as PrintChat (it calls it internally), it only add a entry to the queue,
        /// later to be processed when UpdateQueue() is called.
        /// </summary>
        /// <param name="message">A message to send.</param>
        public void Print(SeString message)
        {
            var configuration = Service<DalamudConfiguration>.Get();

            // Log.Verbose("[CHATGUI PRINT SESTRING]{0}", message.TextValue);
            this.PrintChat(new XivChatEntry
            {
                Message = message,
                Type = configuration.GeneralChatType,
            });
        }

        /// <summary>
        /// Queue an error chat message. While method is named as PrintChat (it calls it internally), it only add a entry to
        /// the queue, later to be processed when UpdateQueue() is called.
        /// </summary>
        /// <param name="message">A message to send.</param>
        public void PrintError(string message)
        {
            // Log.Verbose("[CHATGUI PRINT REGULAR ERROR]{0}", message);
            this.PrintChat(new XivChatEntry
            {
                Message = message,
                Type = XivChatType.Urgent,
            });
        }

        /// <summary>
        /// Queue an error chat message. While method is named as PrintChat (it calls it internally), it only add a entry to
        /// the queue, later to be processed when UpdateQueue() is called.
        /// </summary>
        /// <param name="message">A message to send.</param>
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
                using var senderOwned = Service<LibcFunction>.Get().NewString(senderRaw);

                var messageRaw = (chat.Message ?? string.Empty).Encode();
                using var messageOwned = Service<LibcFunction>.Get().NewString(messageRaw);

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

        private static unsafe bool FastByteArrayCompare(byte[] a1, byte[] a2)
        {
            // Copyright (c) 2008-2013 Hafthor Stefansson
            // Distributed under the MIT/X11 software license
            // Ref: http://www.opensource.org/licenses/mit-license.php.
            // https://stackoverflow.com/a/8808245

            if (a1 == a2) return true;
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                var l = a1.Length;
                for (var i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                {
                    if (*((long*)x1) != *((long*)x2))
                        return false;
                }

                if ((l & 4) != 0)
                {
                    if (*((int*)x1) != *((int*)x2))
                        return false;
                    x1 += 4;
                    x2 += 4;
                }

                if ((l & 2) != 0)
                {
                    if (*((short*)x1) != *((short*)x2))
                        return false;
                    x1 += 2;
                    x2 += 2;
                }

                if ((l & 1) != 0)
                {
                    if (*((byte*)x1) != *((byte*)x2))
                        return false;
                }

                return true;
            }
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
                this.CheckMessageHandled?.Invoke(chattype, senderid, ref parsedSender, ref parsedMessage, ref isHandled);

                if (!isHandled)
                {
                    this.ChatMessage?.Invoke(chattype, senderid, ref parsedSender, ref parsedMessage, ref isHandled);
                }

                var newEdited = parsedMessage.Encode();
                if (!FastByteArrayCompare(oldEdited, newEdited))
                {
                    Log.Verbose("SeString was edited, taking precedence over StdString edit.");
                    message.RawData = newEdited;
                    // Log.Debug($"\nOLD: {BitConverter.ToString(originalMessageData)}\nNEW: {BitConverter.ToString(newEdited)}");
                }

                if (!FastByteArrayCompare(originalMessageData, message.RawData))
                {
                    allocatedString = Service<LibcFunction>.Get().NewString(message.RawData);
                    Log.Debug($"HandlePrintMessageDetour String modified: {originalMessageData}({messagePtr}) -> {message}({allocatedString.Address})");
                    messagePtr = allocatedString.Address;
                }

                var newEditedSender = parsedSender.Encode();
                if (!FastByteArrayCompare(oldEditedSender, newEditedSender))
                {
                    Log.Verbose("SeString was edited, taking precedence over StdString edit.");
                    sender.RawData = newEditedSender;
                    // Log.Debug($"\nOLD: {BitConverter.ToString(originalMessageData)}\nNEW: {BitConverter.ToString(newEdited)}");
                }

                if (!FastByteArrayCompare(originalSenderData, sender.RawData))
                {
                    allocatedStringSender = Service<LibcFunction>.Get().NewString(sender.RawData);
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
}

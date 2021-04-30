using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Internal.Libc;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class ChatGui : IDisposable {
        private readonly Queue<XivChatEntry> chatQueue = new Queue<XivChatEntry>();

        #region Events

        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);
        public delegate void OnMessageRawDelegate(XivChatType type, uint senderId, ref StdString sender, ref StdString message, ref bool isHandled);
        public delegate void OnCheckMessageHandledDelegate(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled);
        public delegate void OnMessageHandledDelegate(XivChatType type, uint senderId, SeString sender, SeString message);
        public delegate void OnMessageUnhandledDelegate(XivChatType type, uint senderId, SeString sender, SeString message);

        /// <summary>
        /// Event that allows you to stop messages from appearing in chat by setting the isHandled parameter to true.
        /// </summary>
        public event OnCheckMessageHandledDelegate OnCheckMessageHandled;

        /// <summary>
        /// Event that will be fired when a chat message is sent to chat by the game.
        /// </summary>
        public event OnMessageDelegate OnChatMessage;

        /// <summary>
        /// Event that will be fired when a chat message is handled by Dalamud or a Plugin.
        /// </summary>
        public event OnMessageHandledDelegate OnChatMessageHandled;

        /// <summary>
        /// Event that will be fired when a chat message is not handled by Dalamud or a Plugin.
        /// </summary>
        public event OnMessageUnhandledDelegate OnChatMessageUnhandled;

        /// <summary>
        /// Event that will be fired when a chat message is sent by the game, containing raw, unparsed data.
        /// </summary>
        [Obsolete("Please use OnChatMessage instead. For modifications, it will take precedence.")]
        public event OnMessageRawDelegate OnChatMessageRaw;

        #endregion

        #region Hooks

        private readonly Hook<PrintMessageDelegate> printMessageHook;

        private readonly Hook<PopulateItemLinkDelegate> populateItemLinkHook;

        private readonly Hook<InteractableLinkClickedDelegate> interactableLinkClickedHook;

        #endregion

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PrintMessageDelegate(IntPtr manager, XivChatType chatType, IntPtr senderName,
                                                     IntPtr message,
                                                     uint senderId, IntPtr parameter);


        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void PopulateItemLinkDelegate(IntPtr linkObjectPtr, IntPtr itemInfoPtr);


        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void InteractableLinkClickedDelegate(IntPtr managerPtr, IntPtr messagePtr);

        #endregion

        public int LastLinkedItemId { get; private set; }
        public byte LastLinkedItemFlags { get; private set; }

        private ChatGuiAddressResolver Address { get; }

        private IntPtr baseAddress = IntPtr.Zero;

        private readonly Dalamud dalamud;

        public ChatGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud) {
            this.dalamud = dalamud;

            Address = new ChatGuiAddressResolver(baseAddress);
            Address.Setup(scanner);

            Log.Verbose("Chat manager address {ChatManager}", Address.BaseAddress);

            this.printMessageHook =
                new Hook<PrintMessageDelegate>(Address.PrintMessage, new PrintMessageDelegate(HandlePrintMessageDetour),
                                               this);
            this.populateItemLinkHook =
                new Hook<PopulateItemLinkDelegate>(Address.PopulateItemLinkObject,
                                                   new PopulateItemLinkDelegate(HandlePopulateItemLinkDetour),
                                                   this);
            this.interactableLinkClickedHook =
                new Hook<InteractableLinkClickedDelegate>(Address.InteractableLinkClicked,
                                                          new InteractableLinkClickedDelegate(InteractableLinkClickedDetour));

        }

        public void Enable() {
            this.printMessageHook.Enable();
            this.populateItemLinkHook.Enable();
            this.interactableLinkClickedHook.Enable();
        }

        public void Dispose() {
            this.printMessageHook.Dispose();
            this.populateItemLinkHook.Dispose();
            this.interactableLinkClickedHook.Dispose();
        }

        private void HandlePopulateItemLinkDetour(IntPtr linkObjectPtr, IntPtr itemInfoPtr) {
            try {
                this.populateItemLinkHook.Original(linkObjectPtr, itemInfoPtr);

                LastLinkedItemId = Marshal.ReadInt32(itemInfoPtr, 8);
                LastLinkedItemFlags = Marshal.ReadByte(itemInfoPtr, 0x14);

                Log.Debug($"HandlePopulateItemLinkDetour {linkObjectPtr} {itemInfoPtr} - linked:{LastLinkedItemId}");
            } catch (Exception ex) {
                Log.Error(ex, "Exception onPopulateItemLink hook.");
                this.populateItemLinkHook.Original(linkObjectPtr, itemInfoPtr);
            }
        }

        private IntPtr HandlePrintMessageDetour(IntPtr manager, XivChatType chattype, IntPtr pSenderName, IntPtr pMessage,
                                              uint senderid, IntPtr parameter) {
            var retVal = IntPtr.Zero;

            try {
                var sender = StdString.ReadFromPointer(pSenderName);
                var message = StdString.ReadFromPointer(pMessage);

                var parsedSender = this.dalamud.SeStringManager.Parse(sender.RawData);
                var parsedMessage = this.dalamud.SeStringManager.Parse(message.RawData);

                Log.Verbose("[CHATGUI][{0}][{1}]", parsedSender.TextValue, parsedMessage.TextValue);

                //Log.Debug($"HandlePrintMessageDetour {manager} - [{chattype}] [{BitConverter.ToString(message.RawData).Replace("-", " ")}] {message.Value} from {senderName.Value}");

                var originalMessageData = (byte[]) message.RawData.Clone();
                var oldEdited = parsedMessage.Encode();

                // Call events
                var isHandled = false;
                OnCheckMessageHandled?.Invoke(chattype, senderid, ref parsedSender, ref parsedMessage, ref isHandled);

                if (!isHandled) {
                    OnChatMessage?.Invoke(chattype, senderid, ref parsedSender, ref parsedMessage, ref isHandled);
                    OnChatMessageRaw?.Invoke(chattype, senderid, ref sender, ref message, ref isHandled);
                }

                var newEdited = parsedMessage.Encode();

                if (!FastByteArrayCompare(oldEdited, newEdited)) {
                    Log.Verbose("SeString was edited, taking precedence over StdString edit.");
                    message.RawData = newEdited;
                    Log.Debug($"\nOLD: {BitConverter.ToString(originalMessageData)}\nNEW: {BitConverter.ToString(newEdited)}");
                } 

                var messagePtr = pMessage;
                OwnedStdString allocatedString = null;

                if (!FastByteArrayCompare(originalMessageData, message.RawData)) {
                    allocatedString = this.dalamud.Framework.Libc.NewString(message.RawData);
                    Log.Debug(
                        $"HandlePrintMessageDetour String modified: {originalMessageData}({messagePtr}) -> {message}({allocatedString.Address})");
                    messagePtr = allocatedString.Address;
                }

                // Print the original chat if it's handled.
                if (isHandled)
                {
                    OnChatMessageHandled?.Invoke(chattype, senderid, parsedSender, parsedMessage);
                }
                else
                {
                    retVal = this.printMessageHook.Original(manager, chattype, pSenderName, messagePtr, senderid, parameter);
                    OnChatMessageUnhandled?.Invoke(chattype, senderid, parsedSender, parsedMessage);
                }

                if (this.baseAddress == IntPtr.Zero)
                    this.baseAddress = manager;

                allocatedString?.Dispose();
            } catch (Exception ex) {
                Log.Error(ex, "Exception on OnChatMessage hook.");
                retVal = this.printMessageHook.Original(manager, chattype, pSenderName, pMessage, senderid, parameter);
            }

            return retVal;
        }

        private readonly Dictionary<(string pluginName, uint commandId), Action<uint, SeString>> dalamudLinkHandlers = new Dictionary<(string, uint), Action<uint, SeString>>();

        /// <summary>
        /// Create a link handler
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="commandId"></param>
        /// <param name="commandAction"></param>
        /// <returns></returns>
        internal DalamudLinkPayload AddChatLinkHandler(string pluginName, uint commandId, Action<uint, SeString> commandAction) {
            var payload = new DalamudLinkPayload() {Plugin = pluginName, CommandId = commandId};
            this.dalamudLinkHandlers.Add((pluginName, commandId), commandAction);
            return payload;
        }

        /// <summary>
        /// Remove a registered link handler
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="commandId"></param>
        internal void RemoveChatLinkHandler(string pluginName, uint commandId) {
            if (this.dalamudLinkHandlers.ContainsKey((pluginName, commandId))) {
                this.dalamudLinkHandlers.Remove((pluginName, commandId));
            }
        }

        /// <summary>
        /// Remove all handlers owned by a plugin.
        /// </summary>
        /// <param name="pluginName"></param>
        internal void RemoveChatLinkHandler(string pluginName) {
            foreach (var handler in this.dalamudLinkHandlers.Keys.ToList().Where(k => k.pluginName == pluginName)) {
                this.dalamudLinkHandlers.Remove(handler);
            }
        }

        private void InteractableLinkClickedDetour(IntPtr managerPtr, IntPtr messagePtr) {
            try {
                var interactableType = (Payload.EmbeddedInfoType)(Marshal.ReadByte(messagePtr, 0x1B) + 1);

                if (interactableType != Payload.EmbeddedInfoType.DalamudLink) {
                    this.interactableLinkClickedHook.Original(managerPtr, messagePtr);
                    return;
                }

                Log.Verbose($"InteractableLinkClicked: {Payload.EmbeddedInfoType.DalamudLink}");

                var payloadPtr = Marshal.ReadIntPtr(messagePtr, 0x10);
                var messageSize = 0;
                while (Marshal.ReadByte(payloadPtr, messageSize) != 0) messageSize++;
                var payloadBytes = new byte[messageSize];
                Marshal.Copy(payloadPtr, payloadBytes, 0, messageSize);
                var seStr = this.dalamud.SeStringManager.Parse(payloadBytes);
                var terminatorIndex = seStr.Payloads.IndexOf(RawPayload.LinkTerminator);
                var payloads = terminatorIndex >= 0 ? seStr.Payloads.Take(terminatorIndex + 1).ToList() : seStr.Payloads;
                if (payloads.Count == 0) return;
                var linkPayload = payloads[0];
                if (linkPayload is DalamudLinkPayload link) {
                    if (this.dalamudLinkHandlers.ContainsKey((link.Plugin, link.CommandId))) {
                        Log.Verbose($"Sending DalamudLink to {link.Plugin}: {link.CommandId}");
                        this.dalamudLinkHandlers[(link.Plugin, link.CommandId)].Invoke(link.CommandId, new SeString(payloads));
                    } else {
                        Log.Debug($"No DalamudLink registered for {link.Plugin} with ID of {link.CommandId}");
                    }
                }
            } catch (Exception ex) {
                Log.Error(ex, "Exception on InteractableLinkClicked hook");
            }
        }

        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        // https://stackoverflow.com/a/8808245
        static unsafe bool FastByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1 == a2) return true;
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2)) return false;
                if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }

        /// <summary>
        ///     Queue a chat message. While method is named as PrintChat, it only add a entry to the queue,
        ///     later to be processed when UpdateQueue() is called.
        /// </summary>
        /// <param name="chat">A message to send.</param>
        public void PrintChat(XivChatEntry chat) {
            this.chatQueue.Enqueue(chat);
        }

        public void Print(string message) {
            Log.Verbose("[CHATGUI PRINT REGULAR]{0}", message);
            PrintChat(new XivChatEntry {
                MessageBytes = Encoding.UTF8.GetBytes(message),
                Type = this.dalamud.Configuration.GeneralChatType
            });
        }

        public void Print(SeString message) {
            Log.Verbose("[CHATGUI PRINT SESTRING]{0}", message.TextValue);
            PrintChat(new XivChatEntry {
                MessageBytes = message.Encode(),
                Type = this.dalamud.Configuration.GeneralChatType
            });
        }

        public void PrintError(string message) {
            Log.Verbose("[CHATGUI PRINT REGULAR ERROR]{0}", message);
            PrintChat(new XivChatEntry {
                MessageBytes = Encoding.UTF8.GetBytes(message),
                Type = XivChatType.Urgent
            });
        }

        public void PrintError(SeString message) {
            Log.Verbose("[CHATGUI PRINT SESTRING ERROR]{0}", message.TextValue);
            PrintChat(new XivChatEntry {
                MessageBytes = message.Encode(),
                Type = XivChatType.Urgent
            });
        }

        /// <summary>
        ///     Process a chat queue.
        /// </summary>
        public void UpdateQueue(Framework framework) {
            while (this.chatQueue.Count > 0) {
                var chat = this.chatQueue.Dequeue();

                if (this.baseAddress == IntPtr.Zero) {
                    continue;
                }

                var senderRaw = Encoding.UTF8.GetBytes(chat.Name ?? "");
                using var senderOwned = framework.Libc.NewString(senderRaw);

                var messageRaw = chat.MessageBytes ?? new byte[0];
                using var messageOwned = framework.Libc.NewString(messageRaw);

                this.HandlePrintMessageDetour(this.baseAddress, chat.Type, senderOwned.Address, messageOwned.Address, chat.SenderId, chat.Parameters);
            }
        }
    }
}

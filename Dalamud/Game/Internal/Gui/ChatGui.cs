using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.Chat;
using Dalamud.Game.Internal.Libc;
using Dalamud.Hooking;
using Discord.Rest;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class ChatGui : IDisposable {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr PrintMessageDelegate(IntPtr manager, XivChatType chatType, IntPtr senderName,
                                                   IntPtr message,
                                                   uint senderId, IntPtr parameter);

        public delegate void OnMessageDelegate(XivChatType type, uint senderId, ref StdString sender, ref StdString message,
                                               ref bool isHandled);


        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void PopulateItemLinkDelegate(IntPtr linkObjectPtr, IntPtr itemInfoPtr);

        private readonly Queue<XivChatEntry> chatQueue = new Queue<XivChatEntry>();

        private readonly Hook<PrintMessageDelegate> printMessageHook;

        public event OnMessageDelegate OnChatMessage;

        private readonly Hook<PopulateItemLinkDelegate> populateItemLinkHook;

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
        }

        public void Enable() {
            this.printMessageHook.Enable();
            this.populateItemLinkHook.Enable();
        }

        public void Dispose() {
            this.printMessageHook.Dispose();
            this.populateItemLinkHook.Dispose();
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
            IntPtr retVal = IntPtr.Zero;

            try {
                var senderName = StdString.ReadFromPointer(pSenderName);
                var message = StdString.ReadFromPointer(pMessage);

                Log.Debug($"HandlePrintMessageDetour {manager} - [{chattype}] [{BitConverter.ToString(message.RawData).Replace("-", " ")}] {message.Value} from {senderName.Value}");
                // Log.Debug($"Got message bytes {BitConverter.ToString(messageBytes.Bytes).Replace("-", " ")}");

                var originalMessageData = (byte[]) message.RawData.Clone();

                // Call events
                var isHandled = false;
                OnChatMessage?.Invoke(chattype, senderid, ref senderName, ref message, ref isHandled);

                var messagePtr = pMessage;
                OwnedStdString allocatedString = null;  

                if (!FastByteArrayCompare(originalMessageData, message.RawData)) {
                    allocatedString = this.dalamud.Framework.Libc.NewString(message.RawData);
                    Log.Debug(
                        $"HandlePrintMessageDetour String modified: {originalMessageData}({messagePtr}) -> {message}({allocatedString.Address})");
                    messagePtr = allocatedString.Address;
                }

                // Print the original chat if it's handled.
                if (!isHandled)
                    retVal = this.printMessageHook.Original(manager, chattype, pSenderName, messagePtr, senderid, parameter);

                if (this.baseAddress == IntPtr.Zero)
                    this.baseAddress = manager;

                allocatedString?.Dispose();
            } catch (Exception ex) {
                Log.Error(ex, "Exception on OnChatMessage hook.");
                retVal = this.printMessageHook.Original(manager, chattype, pSenderName, pMessage, senderid, parameter);
            }

            return retVal;
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
            PrintChat(new XivChatEntry {
                MessageBytes = Encoding.UTF8.GetBytes(message)
            });
        }

        public void PrintError(string message) {
            PrintChat(new XivChatEntry {
                MessageBytes = Encoding.UTF8.GetBytes(message),
                Type = XivChatType.Urgent
            });
        }

        /// <summary>
        ///     Process a chat queue.
        /// </summary>
        public void UpdateQueue(Framework framework) {
            while (this.chatQueue.Count > 0) {
                var chat = this.chatQueue.Dequeue();

                var sender = chat.Name ?? "";
                var message = chat.MessageBytes ?? new byte[0];

                if (this.baseAddress != IntPtr.Zero)
                    using (var senderVec = framework.Libc.NewString(Encoding.UTF8.GetBytes(sender)))
                    using (var messageVec = framework.Libc.NewString(message))
                    {
                        Log.Verbose($"String allocated to {messageVec.Address.ToInt64():X}");
                        this.printMessageHook.Original(this.baseAddress, chat.Type, senderVec.Address,
                                                       messageVec.Address, chat.SenderId, chat.Parameters);
                    }
            }
        }
    }
}

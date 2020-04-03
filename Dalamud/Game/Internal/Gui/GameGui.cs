using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Chat;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class GameGui : IDisposable {
        private GameGuiAddressResolver Address { get; }
        
        public ChatGui Chat { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SetGlobalBgmDelegate(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6);
        private readonly Hook<SetGlobalBgmDelegate> setGlobalBgmHook;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr HandleItemHoverDelegate(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4);
        private readonly Hook<HandleItemHoverDelegate> handleItemHoverHook;

        /// <summary>
        /// The item ID that is currently hovered by the player. 0 when no item is hovered.
        /// If > 1.000.000, subtract 1.000.000 and treat it as HQ
        /// </summary>
        public ulong HoveredItem { get; set; }
        
        /// <summary>
        /// Event that is fired when the currently hovered item changes.
        /// </summary>
        public EventHandler<ulong> HoveredItemChanged { get; set; }

        public GameGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud) {
            Address = new GameGuiAddressResolver(baseAddress);
            Address.Setup(scanner);

            Log.Verbose("===== G A M E G U I =====");

            Log.Verbose("GameGuiManager address {Address}", Address.BaseAddress);
            Log.Verbose("SetGlobalBgm address {Address}", Address.SetGlobalBgm);
            Log.Verbose("HandleItemHover address {Address}", Address.HandleItemHover);

            Chat = new ChatGui(Address.ChatManager, scanner, dalamud);

            this.setGlobalBgmHook =
                new Hook<SetGlobalBgmDelegate>(Address.SetGlobalBgm,
                                                   new SetGlobalBgmDelegate(HandleSetGlobalBgmDetour),
                                                   this);
            this.handleItemHoverHook =
                new Hook<HandleItemHoverDelegate>(Address.HandleItemHover,
                                               new HandleItemHoverDelegate(HandleItemHoverDetour),
                                               this);
        }

        private IntPtr HandleSetGlobalBgmDetour(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6) {
            var retVal = this.setGlobalBgmHook.Original(bgmKey, a2, a3, a4, a5, a6);

            Log.Verbose("SetGlobalBgm: {0} {1} {2} {3} {4} {5} -> {6}", bgmKey, a2, a3, a4, a5, a6, retVal);

            return retVal;
        }

        private IntPtr HandleItemHoverDetour(IntPtr hoverState, IntPtr a2, IntPtr a3, ulong a4) {
            var retVal = this.handleItemHoverHook.Original(hoverState, a2, a3, a4);

            if (retVal.ToInt64() == 22) {
                var itemId = (ulong)Marshal.ReadInt32(hoverState, 0x130);

                try {
                    HoveredItemChanged?.Invoke(this, itemId);
                } catch (Exception e) {
                    Log.Error(e, "Could not dispatch HoveredItemChanged event.");
                }

                Log.Verbose("HoverItemId: {0}", itemId);
            }

            return retVal;
        }

        public void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0); 

        public void Enable() {
            Chat.Enable();
            this.setGlobalBgmHook.Enable();
            this.handleItemHoverHook.Enable();
        }

        public void Dispose() {
            Chat.Dispose();
            this.setGlobalBgmHook.Dispose();
            this.handleItemHoverHook.Dispose();
        }
    }
}

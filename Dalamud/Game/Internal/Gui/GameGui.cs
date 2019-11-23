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

        public GameGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud) {
            Address = new GameGuiAddressResolver(baseAddress);
            Address.Setup(scanner);

            Log.Verbose("===== G A M E G U I =====");

            Log.Verbose("GameGuiManager address {Address}", Address.BaseAddress);
            Log.Verbose("SetGlobalBgm address {Address}", Address.SetGlobalBgm);

            Chat = new ChatGui(Address.ChatManager, scanner, dalamud);

            this.setGlobalBgmHook =
                new Hook<SetGlobalBgmDelegate>(Address.SetGlobalBgm,
                                                   new SetGlobalBgmDelegate(HandleSetGlobalBgmDetour),
                                                   this);
        }

        private IntPtr HandleSetGlobalBgmDetour(UInt16 bgmKey, byte a2, UInt32 a3, UInt32 a4, UInt32 a5, byte a6) {
            var retVal = this.setGlobalBgmHook.Original(bgmKey, a2, a3, a4, a5, a6);

            Log.Verbose("SetGlobalBgm: {0} {1} {2} {3} {4} {5} -> {6}", bgmKey, a2, a3, a4, a5, a6, retVal);

            return retVal;
        }

        public void SetBgm(ushort bgmKey) => this.setGlobalBgmHook.Original(bgmKey, 0, 0, 0, 0, 0); 

        public void Enable() {
            Chat.Enable();
            this.setGlobalBgmHook.Enable();
        }

        public void Dispose() {
            Chat.Dispose();
            this.setGlobalBgmHook.Dispose();
        }
    }
}

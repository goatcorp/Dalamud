using System;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class GameGuiAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; private set; }
        
        public IntPtr ChatManager { get; private set; }

        public IntPtr SetGlobalBgm { get; private set; }
        
        public GameGuiAddressResolver(IntPtr baseAddress) {
            BaseAddress = baseAddress;
        }
        
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetChatManagerDelegate(IntPtr guiManager);

        protected override void SetupInternal(SigScanner scanner) {
            // Xiv__UiManager__GetChatManager   000   lea     rax, [rcx+13E0h]
            // Xiv__UiManager__GetChatManager+7 000   retn
            ChatManager = BaseAddress + 0x13E0;
        }

        protected override void Setup64Bit(SigScanner sig) {
            //SetGlobalBgm = sig.ScanText("4C 8B 15 ?? ?? ?? ?? 4D 85 D2 74 58 41 83 7A ?? ?? 76 51 4D 8B 92 ?? ?? ?? ?? 0F B6 44 24 ?? 49 81 C2 ?? ?? ?? ?? 66 41 89 4A ?? 33 C9 41 88 52 30 41 89 4A 14 66 41 89 4A ?? 41 88 42 12 49 89 4A 38 41 89 4A 40 49 89 4A 48 41 38 4A 30 74 14 8B 44 24 28 41 89 42 40 45 89 42 38");
            SetGlobalBgm = sig.ScanText("4C 8B 15 ?? ?? ?? ?? 4D 85 D2 74 58");
        }
    }
}

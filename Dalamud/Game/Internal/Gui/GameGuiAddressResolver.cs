using System;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class GameGuiAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; private set; }
        
        public IntPtr ChatManager { get; private set; }

        public IntPtr SetGlobalBgm { get; private set; }
        public IntPtr HandleItemHover { get; set; }
        
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
            SetGlobalBgm = sig.ScanText("4C 8B 15 ?? ?? ?? ?? 4D 85 D2 74 58");
            HandleItemHover = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 89 AE ?? ?? ?? ??");
        }
    }
}

using System;
using System.Runtime.InteropServices;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    internal sealed class GameGuiAddressResolver : BaseAddressResolver {
        public IntPtr BaseAddress { get; private set; }
        
        public IntPtr ChatManager { get; private set; }

        public IntPtr SetGlobalBgm { get; private set; }
        public IntPtr HandleItemHover { get; set; }
        public IntPtr HandleItemOut { get; set; }
        public IntPtr GetUIObject { get; private set; }
        public IntPtr GetMatrixSingleton { get; private set; }
        public IntPtr ScreenToWorld { get; private set; }
        public IntPtr ToggleUiHide { get; set; }

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
            HandleItemOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 4D");
            GetUIObject = sig.ScanText("E8 ?? ?? ?? ?? 48 8B C8 48 8B 10 FF 52 40 80 88 ?? ?? ?? ?? 01 E9");
            GetMatrixSingleton = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
            ScreenToWorld = sig.ScanText("48 83 EC 48 48 8B 05 ?? ?? ?? ?? 4D 8B D1");
            ToggleUiHide = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 B9 ?? ?? ?? ?? B8 ?? ?? ?? ??");
        }
    }
}

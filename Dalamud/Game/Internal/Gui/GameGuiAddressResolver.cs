using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.Internal.Gui
{
    internal sealed class GameGuiAddressResolver : BaseAddressResolver
    {
        public IntPtr BaseAddress { get; private set; }

        public IntPtr ChatManager { get; private set; }

        public IntPtr SetGlobalBgm { get; private set; }

        public IntPtr HandleItemHover { get; set; }

        public IntPtr HandleItemOut { get; set; }

        public IntPtr HandleActionHover { get; set; }

        public IntPtr HandleActionOut { get; set; }

        public IntPtr GetUIObject { get; private set; }

        public IntPtr GetMatrixSingleton { get; private set; }

        public IntPtr ScreenToWorld { get; private set; }

        public IntPtr ToggleUiHide { get; set; }

        public IntPtr GetBaseUIObject { get; private set; }

        public IntPtr GetUIObjectByName { get; private set; }

        public IntPtr GetUIModule { get; private set; }

        public IntPtr GetAgentModule { get; private set; }

        public GameGuiAddressResolver(IntPtr baseAddress)
        {
            this.BaseAddress = baseAddress;
        }

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetChatManagerDelegate(IntPtr guiManager);

        protected override void SetupInternal(SigScanner scanner)
        {
            // Xiv__UiManager__GetChatManager   000   lea     rax, [rcx+13E0h]
            // Xiv__UiManager__GetChatManager+7 000   retn
            this.ChatManager = this.BaseAddress + 0x13E0;
        }

        protected override void Setup64Bit(SigScanner sig)
        {
            this.SetGlobalBgm = sig.ScanText("4C 8B 15 ?? ?? ?? ?? 4D 85 D2 74 58");
            this.HandleItemHover = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 89 AE ?? ?? ?? ??");
            this.HandleItemOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 4D");
            this.HandleActionHover = sig.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 83 F8 0F");
            this.HandleActionOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8B F9 4D 85 C0 74 1F");
            this.GetUIObject = sig.ScanText("E8 ?? ?? ?? ?? 48 8B C8 48 8B 10 FF 52 40 80 88 ?? ?? ?? ?? 01 E9");
            this.GetMatrixSingleton = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
            this.ScreenToWorld = sig.ScanText("48 83 EC 48 48 8B 05 ?? ?? ?? ?? 4D 8B D1");
            this.ToggleUiHide = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 B9 ?? ?? ?? ?? B8 ?? ?? ?? ??");
            this.GetBaseUIObject = sig.ScanText("E8 ?? ?? ?? ?? 41 B8 01 00 00 00 48 8D 15 ?? ?? ?? ?? 48 8B 48 20 E8 ?? ?? ?? ?? 48 8B CF");
            this.GetUIObjectByName = sig.ScanText("E8 ?? ?? ?? ?? 48 8B CF 48 89 87 ?? ?? 00 00 E8 ?? ?? ?? ?? 41 B8 01 00 00 00");
            this.GetUIModule = sig.ScanText("E8 ?? ?? ?? ?? 48 8B C8 48 85 C0 75 2D");

            var uiModuleVtableSig = sig.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 4C 89 61 28");
            this.GetAgentModule = Marshal.ReadIntPtr(uiModuleVtableSig, 34 * IntPtr.Size);
        }
    }
}

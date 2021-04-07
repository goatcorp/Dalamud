using System;

namespace Dalamud.Game.Internal.Gui
{
    public class ToastGuiAddressResolver : BaseAddressResolver
    {
        public IntPtr ShowNormalToast { get; private set; }

        public IntPtr ShowQuestToast { get; private set; }

        public IntPtr ShowErrorToast { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            this.ShowNormalToast = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 83 3D ?? ?? ?? ?? ??");
            this.ShowQuestToast = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 40 83 3D ?? ?? ?? ?? ??");
            this.ShowErrorToast = sig.ScanText("40 56 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 41 8B F0");
        }
    }
}

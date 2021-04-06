using System;

namespace Dalamud.Game.Internal.Gui
{
    public class ToastGuiAddressResolver : BaseAddressResolver
    {
        public IntPtr ShowToast { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            ShowToast = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 83 3D ?? ?? ?? ?? ??");
        }
    }
}

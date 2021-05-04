using System;

namespace Dalamud.Game.Internal.Gui
{
    public class FlyTextGuiAddressResolver : BaseAddressResolver
    {
        public IntPtr AddFlyText { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            this.AddFlyText = sig.ScanText("4C 8B DC 4D 89 4B 20 57 41 54 41 55 48 81 EC ?? ?? ?? ?? 8B C2 49 8B F9");
        }
    }
}

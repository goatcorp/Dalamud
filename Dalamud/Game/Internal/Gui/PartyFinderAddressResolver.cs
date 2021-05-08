using System;

namespace Dalamud.Game.Internal.Gui
{
    internal class PartyFinderAddressResolver : BaseAddressResolver
    {
        public IntPtr ReceiveListing { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            this.ReceiveListing = sig.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");
        }
    }
}

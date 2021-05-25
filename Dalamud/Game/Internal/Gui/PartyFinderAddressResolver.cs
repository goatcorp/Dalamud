using System;

namespace Dalamud.Game.Internal.Gui
{
    /// <summary>
    /// The address resolver for the <see cref="PartyFinderGui"/> class.
    /// </summary>
    internal class PartyFinderAddressResolver : BaseAddressResolver
    {
        /// <summary>
        /// Gets the address of the native ReceiveListing method.
        /// </summary>
        public IntPtr ReceiveListing { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner sig)
        {
            this.ReceiveListing = sig.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");
        }
    }
}

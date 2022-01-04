using System;

namespace Dalamud.Game.Gui.FellowshipFinder
{
    /// <summary>
    /// The address resolver for the <see cref="FellowshipFinderGui"/> class.
    /// </summary>
    public class FellowshipFinderAddressResolver : BaseAddressResolver
    {
        /// <summary>
        /// Gets the address of the native ReceiveListing method.
        /// </summary>
        public IntPtr ReceiveListing { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner sig)
        {
            this.ReceiveListing = sig.ScanText("4C 8B DC 55 56 41 54 41 55 41 56 41 57");
        }
    }
}

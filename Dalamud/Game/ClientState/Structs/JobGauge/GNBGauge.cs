using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory GNB job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct GNBGauge
    {
        [FieldOffset(0)]
        private byte numAmmo;

        [FieldOffset(2)]
        private short maxTimerDuration;

        [FieldOffset(4)]
        private byte ammoComboStepNumber;

        /// <summary>
        /// Gets the amount of ammo available.
        /// </summary>
        public byte NumAmmo => this.numAmmo;

        /// <summary>
        /// Gets the max combo time of the Gnashing Fang combo.
        /// </summary>
        public short MaxTimerDuration => this.maxTimerDuration;

        /// <summary>
        /// Gets the current step of the Gnashing Fang combo.
        /// </summary>
        public byte AmmoComboStepNumber => this.ammoComboStepNumber;
    }
}

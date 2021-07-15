using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory GNB job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct GNBGauge
    {
        /// <summary>
        /// Gets the amount of ammo available.
        /// </summary>
        [FieldOffset(0)]
        public byte NumAmmo;

        /// <summary>
        /// Gets the max combo time of the Gnashing Fang combo.
        /// </summary>
        [FieldOffset(2)]
        public short MaxTimerDuration;

        /// <summary>
        /// Gets the current step of the Gnashing Fang combo.
        /// </summary>
        [FieldOffset(4)]
        public byte AmmoComboStepNumber;
    }
}

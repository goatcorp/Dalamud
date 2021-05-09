using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SCH job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SCHGauge
    {
        /// <summary>
        /// Gets the amount of Aetherflow stacks available.
        /// </summary>
        [FieldOffset(2)]
        public byte NumAetherflowStacks;

        /// <summary>
        /// Gets the current level of the Fairy Gauge.
        /// </summary>
        [FieldOffset(3)]
        public byte FairyGaugeAmount;

        /// <summary>
        /// Gets the Seraph time remaining in milliseconds.
        /// </summary>
        [FieldOffset(4)]
        public short SeraphTimer;

        /// <summary>
        /// Gets the last dismissed fairy.
        /// </summary>
        [FieldOffset(6)]
        public DismissedFairy DismissedFairy;
    }
}

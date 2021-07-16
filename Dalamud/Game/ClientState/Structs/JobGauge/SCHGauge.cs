using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory SCH job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct SCHGauge
    {
        [FieldOffset(2)]
        private byte numAetherflowStacks;

        [FieldOffset(3)]
        private byte fairyGaugeAmount;

        [FieldOffset(4)]
        private short seraphTimer;

        [FieldOffset(6)]
        private DismissedFairy dismissedFairy;

        /// <summary>
        /// Gets the amount of Aetherflow stacks available.
        /// </summary>
        public byte NumAetherflowStacks => this.numAetherflowStacks;

        /// <summary>
        /// Gets the current level of the Fairy Gauge.
        /// </summary>
        public byte FairyGaugeAmount => this.fairyGaugeAmount;

        /// <summary>
        /// Gets the Seraph time remaining in milliseconds.
        /// </summary>
        public short SeraphTimer => this.seraphTimer;

        /// <summary>
        /// Gets the last dismissed fairy.
        /// </summary>
        public DismissedFairy DismissedFairy => this.dismissedFairy;
    }
}

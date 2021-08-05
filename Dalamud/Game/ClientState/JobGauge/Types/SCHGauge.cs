using System;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory SCH job gauge.
    /// </summary>
    public unsafe class SCHGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.SCHGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SCHGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal SCHGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the amount of Aetherflow stacks available.
        /// </summary>
        public byte NumAetherflowStacks => this.Struct->NumAetherflowStacks;

        /// <summary>
        /// Gets the current level of the Fairy Gauge.
        /// </summary>
        public byte FairyGaugeAmount => this.Struct->FairyGaugeAmount;

        /// <summary>
        /// Gets the Seraph time remaiSCHg in milliseconds.
        /// </summary>
        public short SeraphTimer => this.Struct->SeraphTimer;

        /// <summary>
        /// Gets the last dismissed fairy.
        /// </summary>
        public DismissedFairy DismissedFairy => (DismissedFairy)this.Struct->DismissedFairy;
    }
}

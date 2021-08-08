using System;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory RDM job gauge.
    /// </summary>
    public unsafe class RDMGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.RDMGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RDMGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal RDMGauge(IntPtr address)
                : base(address)
        {
        }

        /// <summary>
        /// Gets the level of the White gauge.
        /// </summary>
        public byte WhiteGauge => this.Struct->WhiteGauge;

        /// <summary>
        /// Gets the level of the Black gauge.
        /// </summary>
        public byte BlackGauge => this.Struct->BlackGauge;
    }
}

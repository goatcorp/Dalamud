using System;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory MNK job gauge.
    /// </summary>
    public unsafe class MNKGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.MNKGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MNKGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal MNKGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the number of Chakra available.
        /// </summary>
        public byte NumChakra => this.Struct->NumChakra;
    }
}

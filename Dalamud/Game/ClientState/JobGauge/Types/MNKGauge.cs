using System;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory MNK job gauge.
    /// </summary>
    public unsafe class MNKGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.MonkGauge>
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
        /// Gets the number of Chakra available, per Chakra type.
        /// </summary>
        public ChakraType[] Chakra => this.Struct->CurrentChakra;

        /// <summary>
        /// Gets the kind of Nadi available.
        /// </summary>
        public NadiFlags Nadi => this.Struct->Nadi;
    }
}

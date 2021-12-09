using Dalamud.Game.ClientState.JobGauge.Enums;
using System;
using System.Linq;

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
        /// Gets the types of Chakra available.
        /// </summary>
        /// <remarks>
        /// This will always return an array of size 3, inactive Chakra are represented by <see cref="Chakra.NONE"/>.
        /// </remarks>
        public Chakra[] Chakra => this.Struct->CurrentChakra.Select(c => (Chakra)c).ToArray();

        /// <summary>
        /// Gets the types of Nadi available.
        /// </summary>
        public Nadi Nadi => (Nadi)this.Struct->Nadi;

        /// <summary>
        /// Gets the time remaining that Blitz is active.
        /// </summary>
        public ushort BlitzTimeRemaining => this.Struct->BlitzTimeRemaining;
    }
}

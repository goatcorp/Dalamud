using System;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory RPR job gauge.
    /// </summary>
    public unsafe class RPRGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.ReaperGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RPRGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal RPRGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the amount of Soul available.
        /// </summary>
        public byte Soul => this.Struct->Soul;
    }
}

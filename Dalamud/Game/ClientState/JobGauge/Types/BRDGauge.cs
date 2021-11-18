using System;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory BRD job gauge.
    /// </summary>
    public unsafe class BRDGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.BardGauge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BRDGauge"/> class.
        /// </summary>
        /// <param name="address">Address of the job gauge.</param>
        internal BRDGauge(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the current song timer in milliseconds.
        /// </summary>
        public short SongTimer => this.Struct->SongTimer;

        /// <summary>
        /// Gets the amount of Repertoire accumulated.
        /// </summary>
        public byte Repertoire => this.Struct->Repertoire;

        /// <summary>
        /// Gets the amount of Soul Voice accumulated.
        /// </summary>
        public byte SoulVoice => this.Struct->SoulVoice;

        /// <summary>
        /// Gets the type of song that is active.
        /// </summary>
        public Song Song => (Song)this.Struct->Song;
    }
}

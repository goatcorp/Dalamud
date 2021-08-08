using System;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types
{
    /// <summary>
    /// In-memory BRD job gauge.
    /// </summary>
    public unsafe class BRDGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.BRDGauge>
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
        /// Gets the number of stacks for the current song.
        /// </summary>
        public byte NumSongStacks => this.Struct->NumSongStacks;

        /// <summary>
        /// Gets the amount of Soul Voice accumulated.
        /// </summary>
        public byte SoulVoiceValue => this.Struct->SoulVoiceValue;

        /// <summary>
        /// Gets the type of song that is active.
        /// </summary>
        public CurrentSong ActiveSong => (CurrentSong)this.Struct->ActiveSong;
    }
}

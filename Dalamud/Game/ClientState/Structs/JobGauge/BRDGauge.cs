using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory BRD job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BRDGauge
    {
        [FieldOffset(0)]
        private short songTimer;

        [FieldOffset(2)]
        private byte numSongStacks;

        [FieldOffset(3)]
        private byte soulVoiceValue;

        [FieldOffset(4)]
        private CurrentSong activeSong;

        /// <summary>
        /// Gets the current song timer in milliseconds.
        /// </summary>
        public short SongTimer => this.songTimer;

        /// <summary>
        /// Gets the number of stacks for the current song.
        /// </summary>
        public byte NumSongStacks => this.numSongStacks;

        /// <summary>
        /// Gets the amount of Soul Voice accumulated.
        /// </summary>
        public byte SoulVoiceValue => this.soulVoiceValue;

        /// <summary>
        /// Gets the type of song that is active.
        /// </summary>
        public CurrentSong ActiveSong => this.activeSong;
    }
}

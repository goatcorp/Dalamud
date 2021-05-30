using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory BRD job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct BRDGauge
    {
        /// <summary>
        /// Gets the current song timer in milliseconds.
        /// </summary>
        [FieldOffset(0)]
        public short SongTimer;

        /// <summary>
        /// Gets the number of stacks for the current song.
        /// </summary>
        [FieldOffset(2)]
        public byte NumSongStacks;

        /// <summary>
        /// Gets the amount of Soul Voice accumulated.
        /// </summary>
        [FieldOffset(3)]
        public byte SoulVoiceValue;

        /// <summary>
        /// Gets the type of song that is active.
        /// </summary>
        [FieldOffset(4)]
        public CurrentSong ActiveSong;
    }
}

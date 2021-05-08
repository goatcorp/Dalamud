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
        public short SongTimer;

        [FieldOffset(2)]
        public byte NumSongStacks;

        [FieldOffset(3)]
        public byte SoulVoiceValue;

        [FieldOffset(4)]
        public CurrentSong ActiveSong;
    }
}

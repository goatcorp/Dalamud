using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct BRDGauge {
        [FieldOffset(0)] public short SongTimer;
        [FieldOffset(2)] public byte NumSongStacks;
        [FieldOffset(3)] public byte SoulVoiceValue;
        [FieldOffset(4)] public CurrentSong ActiveSong;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct WHMGauge {
        [FieldOffset(2)] public short LilyTimer; //Counts to 30k = 30s
        [FieldOffset(4)] public byte NumLilies;
        [FieldOffset(5)] public byte NumBloodLily;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct NINGauge {
        [FieldOffset(0)] public int HutonTimeLeft;
        [FieldOffset(4)] public byte TCJMudrasUsed; //some sort of mask
        [FieldOffset(5)] public byte Ninki;
        [FieldOffset(6)] public byte NumHutonManualCasts; //wtf
    }
}

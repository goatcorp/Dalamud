using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct SMNGauge {
        
        //Unfinished
        [FieldOffset(0xc)] public short TimerRemaining;
        [FieldOffset(0xf)] public bool IsDemiActive;
        [FieldOffset(0x10)] public byte NumStacks;
    }
}

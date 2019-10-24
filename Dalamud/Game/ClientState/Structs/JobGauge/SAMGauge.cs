using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct SAMGauge {

        [FieldOffset(3)] public byte Kenki;
        [FieldOffset(4)] public Sen Sen;
    }
}

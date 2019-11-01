using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct RDMGauge {
        [FieldOffset(0)] public byte WhiteGauge;
        [FieldOffset(1)] public byte BlackGauge;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct SCHGauge {
        [FieldOffset(2)] public byte NumAetherflowStacks;
        [FieldOffset(3)] public byte FairyGaugeAmount;
        [FieldOffset(4)] public short SeraphTimer;
        [FieldOffset(6)] public DismissedFairy DismissedFairy;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DNCGauge {
        [FieldOffset(0xc)] public byte NumFeathers;
        [FieldOffset(0xd)] public byte Esprit;
        [FieldOffset(0xe)] public fixed byte StepOrder[4];
        [FieldOffset(0x12)] public byte NumCompleteSteps;

        public bool IsDancing() {
            return StepOrder[0] != 0;
        }
    }
}

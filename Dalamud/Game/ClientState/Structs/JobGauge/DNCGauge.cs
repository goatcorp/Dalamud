using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DNCGauge {
        [FieldOffset(0)] public byte NumFeathers;
        [FieldOffset(1)] public byte Esprit;
        [FieldOffset(2)] private fixed byte StepOrder[4];
        [FieldOffset(6)] public byte NumCompleteSteps;

        public bool IsDancing() {
            return StepOrder[0] != 0;
        }

        public ulong NextStep() {
            return (ulong)(15999 + StepOrder[NumCompleteSteps] - 1);
        }
    }
}

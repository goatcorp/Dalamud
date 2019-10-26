using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct MNKGauge {
        [FieldOffset(0)] public byte GLTimer;
        [FieldOffset(2)] public byte NumGLStacks;
        [FieldOffset(3)] public byte NumChakra;
        [FieldOffset(4)] private byte GLTimerFreezeState;

        public bool IsGLTimerFroze() {
            return GLTimerFreezeState > 0;
        }
    }
}

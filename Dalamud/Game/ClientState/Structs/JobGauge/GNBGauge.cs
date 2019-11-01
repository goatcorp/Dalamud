using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct GNBGauge {
        [FieldOffset(0)] public byte NumAmmo;
        [FieldOffset(2)] public short MaxTimerDuration;
        [FieldOffset(4)] public byte AmmoComboStepNumber;
    }
}

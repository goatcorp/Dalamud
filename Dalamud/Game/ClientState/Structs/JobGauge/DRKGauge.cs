using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {
    [StructLayout(LayoutKind.Explicit)]
    public struct DRKGauge {
        [FieldOffset(0)] public short Blood;
        [FieldOffset(2)] public short DarksideTimeRemaining;
        [FieldOffset(4)] public bool HasDarkArts;
        [FieldOffset(6)] public short ShadowTimeRemaining;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {
    [StructLayout(LayoutKind.Explicit)]
    public struct DRKGauge {
        [FieldOffset(0xc)] public short Blood;
        [FieldOffset(0xe)] public short DarksideTimeRemaining;
        [FieldOffset(0x10)] public bool HasDarkArts;
        [FieldOffset(0x12)] public short ShadowTimeRemaining;
    }
}

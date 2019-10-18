using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct BLMGauge {
        [FieldOffset(0xc)] public short TimeUntilNextPolyglot;  //eno timer (ms)
        [FieldOffset(0xe)] public short ElementTimeRemaining;  //ui/af timer
        [FieldOffset(0x10)] private byte ElementStance; //ui/af
        [FieldOffset(0x11)] public byte NumUmbralHearts; //number of umbral hearts
        [FieldOffset(0x12)] public byte NumPolyglotStacks; //number of polyglot stacks
        [FieldOffset(0x13)] public bool IsEnoActive; //eno active?

        public bool InUmbralIce() {
            return ElementStance > 4;
        }

        public bool InAstralFire() {
            return ElementStance > 0 && ElementStance < 4;
        }

    }

    
}

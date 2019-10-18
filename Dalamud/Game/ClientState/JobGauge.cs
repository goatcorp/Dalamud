using Dalamud.Game.ClientState.Structs.JobGauge;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState {
    public static class JobGauge {

        private static IntPtr gaugeStart;

        public static void Init(ProcessModule module) {
            gaugeStart = module.BaseAddress + 0x1b2d4b4;
        }

        // Should only be called with the gauge types in 
        // ClientState.Structs.JobGauge
        public static T Gauge<T>() {
            return Marshal.PtrToStructure<T>(gaugeStart);
        }
    }
}

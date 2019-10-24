using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct MCHGauge{

        [FieldOffset(0)] public short OverheatTimeRemaining;
        [FieldOffset(2)] public short RobotTimeRemaining;
        [FieldOffset(4)] public byte Heat;
        [FieldOffset(5)] public byte Battery;
        [FieldOffset(6)] public byte LastRobotBatteryPower;
        [FieldOffset(7)] private byte TimerActive;

        public bool IsOverheated() {
            return (TimerActive & 1) != 0;
        }
        public bool IsRobotActive() {
            return (TimerActive & 2) != 0;
        }
    }
}

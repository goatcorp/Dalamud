using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.ClientState.Structs.JobGauge {

    [StructLayout(LayoutKind.Explicit)]
    public struct MCHGauge{

        [FieldOffset(0xc)] public short OverheatTimeRemaining;
        [FieldOffset(0xe)] public short RobotTimeRemaining;
        [FieldOffset(0x10)] public byte Heat;
        [FieldOffset(0x11)] public byte Battery;
        [FieldOffset(0x12)] public byte LastRobotBatteryPower;
        [FieldOffset(0x13)] private byte TimerActive;

        public bool IsOverheated() {
            return (TimerActive & 1) != 0;
        }
        public bool IsRobotActive() {
            return (TimerActive & 2) != 0;
        }
    }
}

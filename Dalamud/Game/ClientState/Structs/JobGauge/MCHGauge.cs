using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory MCH job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MCHGauge
    {
        [FieldOffset(0)]
        public short OverheatTimeRemaining;

        [FieldOffset(2)]
        public short RobotTimeRemaining;

        [FieldOffset(4)]
        public byte Heat;

        [FieldOffset(5)]
        public byte Battery;

        [FieldOffset(6)]
        public byte LastRobotBatteryPower;

        [FieldOffset(7)]
        private byte TimerActive;

        public bool IsOverheated()
        {
            return (this.TimerActive & 1) != 0;
        }

        public bool IsRobotActive()
        {
            return (this.TimerActive & 2) != 0;
        }
    }
}

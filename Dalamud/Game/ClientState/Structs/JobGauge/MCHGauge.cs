using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory MCH job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MCHGauge
    {
        /// <summary>
        /// Gets the time time remaining for Overheat in milliseconds.
        /// </summary>
        [FieldOffset(0)]
        public short OverheatTimeRemaining;

        /// <summary>
        /// Gets the time remaining for the Rook or Queen in milliseconds.
        /// </summary>
        [FieldOffset(2)]
        public short RobotTimeRemaining;

        /// <summary>
        /// Gets the current Heat level.
        /// </summary>
        [FieldOffset(4)]
        public byte Heat;

        /// <summary>
        /// Gets the current Battery level.
        /// </summary>
        [FieldOffset(5)]
        public byte Battery;

        /// <summary>
        /// Gets the battery level of the last Robot.
        /// </summary>
        [FieldOffset(6)]
        public byte LastRobotBatteryPower;

        [FieldOffset(7)]
        private byte timerActive;

        /// <summary>
        /// Gets if the player is currently Overheated.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsOverheated() => (this.timerActive & 1) != 0;

        /// <summary>
        /// Gets if the player has an active Robot.
        /// </summary>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public bool IsRobotActive() => (this.timerActive & 2) != 0;
    }
}

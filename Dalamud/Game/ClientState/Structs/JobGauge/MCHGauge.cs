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
        private short overheatTimeRemaining;

        [FieldOffset(2)]
        private short robotTimeRemaining;

        [FieldOffset(4)]
        private byte heat;

        [FieldOffset(5)]
        private byte battery;

        [FieldOffset(6)]
        private byte lastRobotBatteryPower;

        [FieldOffset(7)]
        private byte timerActive;

        /// <summary>
        /// Gets the time time remaining for Overheat in milliseconds.
        /// </summary>
        public short OverheatTimeRemaining => this.overheatTimeRemaining;

        /// <summary>
        /// Gets the time remaining for the Rook or Queen in milliseconds.
        /// </summary>
        public short RobotTimeRemaining => this.robotTimeRemaining;

        /// <summary>
        /// Gets the current Heat level.
        /// </summary>
        public byte Heat => this.heat;

        /// <summary>
        /// Gets the current Battery level.
        /// </summary>
        public byte Battery => this.battery;

        /// <summary>
        /// Gets the battery level of the last Robot.
        /// </summary>
        public byte LastRobotBatteryPower => this.lastRobotBatteryPower;

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

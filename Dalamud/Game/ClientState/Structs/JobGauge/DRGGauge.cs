using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DRG job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DRGGauge
    {
        [FieldOffset(0)]
        private short botdTimer;

        [FieldOffset(2)]
        private BOTDState botdState;

        [FieldOffset(3)]
        private byte eyeCount;

        /// <summary>
        /// Gets the time remaining for Blood of the Dragon in milliseconds.
        /// </summary>
        public short BOTDTimer => this.botdTimer;

        /// <summary>
        /// Gets the current state of Blood of the Dragon.
        /// </summary>
        public BOTDState BOTDState => this.botdState;

        /// <summary>
        /// Gets the count of eyes opened during Blood of the Dragon.
        /// </summary>
        public byte EyeCount => this.eyeCount;
    }
}

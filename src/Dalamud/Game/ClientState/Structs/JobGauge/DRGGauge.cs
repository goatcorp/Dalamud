using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory DRG job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct DRGGauge
    {
        /// <summary>
        /// Gets the time remaining for Blood of the Dragon in milliseconds.
        /// </summary>
        [FieldOffset(0)]
        public short BOTDTimer;

        /// <summary>
        /// Gets the current state of Blood of the Dragon.
        /// </summary>
        [FieldOffset(2)]
        public BOTDState BOTDState;

        /// <summary>
        /// Gets the count of eyes opened during Blood of the Dragon.
        /// </summary>
        [FieldOffset(3)]
        public byte EyeCount;
    }
}

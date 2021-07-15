using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory RDM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RDMGauge
    {
        /// <summary>
        /// Gets the level of the White gauge.
        /// </summary>
        [FieldOffset(0)]
        public byte WhiteGauge;

        /// <summary>
        /// Gets the level of the Black gauge.
        /// </summary>
        [FieldOffset(1)]
        public byte BlackGauge;
    }
}

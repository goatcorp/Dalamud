using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory RDM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RDMGauge
    {
        [FieldOffset(0)]
        private byte whiteGauge;

        [FieldOffset(1)]
        private byte blackGauge;

        /// <summary>
        /// Gets the level of the White gauge.
        /// </summary>
        public byte WhiteGauge => this.whiteGauge;

        /// <summary>
        /// Gets the level of the Black gauge.
        /// </summary>
        public byte BlackGauge => this.blackGauge;
    }
}

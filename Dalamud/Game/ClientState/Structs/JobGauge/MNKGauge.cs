using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory MNK job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MNKGauge
    {
        [FieldOffset(0)]
        private byte numChakra;

        /// <summary>
        /// Gets the number of Chakra available.
        /// </summary>
        public byte NumChakra => this.numChakra;
    }
}

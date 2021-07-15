using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory WHM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WHMGauge
    {
        /// <summary>
        /// Gets the time to next lily in milliseconds.
        /// </summary>
        [FieldOffset(2)]
        public short LilyTimer;

        /// <summary>
        /// Gets the number of Lilies.
        /// </summary>
        [FieldOffset(4)]
        public byte NumLilies;

        /// <summary>
        /// Gets the number of times the blood lily has been nourished.
        /// </summary>
        [FieldOffset(5)]
        public byte NumBloodLily;
    }
}

using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory WHM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WHMGauge
    {
        [FieldOffset(2)]
        private short lilyTimer;

        [FieldOffset(4)]
        private byte numLilies;

        [FieldOffset(5)]
        private byte numBloodLily;

        /// <summary>
        /// Gets the time to next lily in milliseconds.
        /// </summary>
        public short LilyTimer => this.lilyTimer;

        /// <summary>
        /// Gets the number of Lilies.
        /// </summary>
        public byte NumLilies => this.numLilies;

        /// <summary>
        /// Gets the number of times the blood lily has been nourished.
        /// </summary>
        public byte NumBloodLily => this.numBloodLily;
    }
}

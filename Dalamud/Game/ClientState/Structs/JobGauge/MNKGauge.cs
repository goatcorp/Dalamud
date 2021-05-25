using System;
using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory MNK job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct MNKGauge
    {
        /// <summary>
        /// Gets the number of Chakra available.
        /// </summary>
        [FieldOffset(0)]
        public byte NumChakra;

        /// <summary>
        /// Gets the Greased Lightning timer in milliseconds.
        /// </summary>
        [Obsolete("GL has been removed from the game")]
        [FieldOffset(0)]
        public byte GLTimer;

        /// <summary>
        /// Gets the amount of Greased Lightning stacks.
        /// </summary>
        [Obsolete("GL has been removed from the game")]
        [FieldOffset(2)]
        public byte NumGLStacks;

        [Obsolete("GL has been removed from the game")]
        [FieldOffset(4)]
        private byte glTimerFreezeState;

        /// <summary>
        /// Gets if the Greased Lightning timer has been frozen.
        /// </summary>
        /// <returns>><c>true</c> or <c>false</c>.</returns>
        [Obsolete("GL has been removed from the game")]
        public bool IsGLTimerFroze() => false;
    }
}

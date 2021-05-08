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
        [FieldOffset(0)]
        public byte NumChakra;

        [Obsolete("GL has been removed from the game")]
        [FieldOffset(0)]
        public byte GLTimer;

        [Obsolete("GL has been removed from the game")]
        [FieldOffset(2)]
        public byte NumGLStacks;

        [Obsolete("GL has been removed from the game")]
        [FieldOffset(4)]
        private byte GLTimerFreezeState;

        [Obsolete("GL has been removed from the game")]
        public bool IsGLTimerFroze() => false;
    }
}

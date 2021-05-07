using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    [StructLayout(LayoutKind.Explicit)]
    public struct WHMGauge
    {
        [FieldOffset(2)]
        public short LilyTimer; // Counts to 30k = 30s

        [FieldOffset(4)]
        public byte NumLilies;

        [FieldOffset(5)]
        public byte NumBloodLily;
    }
}

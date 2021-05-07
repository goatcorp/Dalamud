using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    [StructLayout(LayoutKind.Explicit)]
    public struct SAMGauge
    {
        [FieldOffset(3)]
        public byte Kenki;

        [FieldOffset(4)]
        public byte MeditationStacks;

        [FieldOffset(5)]
        public Sen Sen;
    }
}

using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DRGGauge
    {
        [FieldOffset(0)]
        public short BOTDTimer;

        [FieldOffset(2)]
        public BOTDState BOTDState;

        [FieldOffset(3)]
        public byte EyeCount;
    }
}

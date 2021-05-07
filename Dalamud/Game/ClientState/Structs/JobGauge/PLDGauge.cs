using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    [StructLayout(LayoutKind.Explicit)]
    public struct PLDGauge
    {
        [FieldOffset(0)]
        public byte GaugeAmount;
    }
}

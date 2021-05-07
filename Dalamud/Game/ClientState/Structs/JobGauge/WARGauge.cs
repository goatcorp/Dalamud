using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    [StructLayout(LayoutKind.Explicit)]
    public struct WARGauge
    {
        [FieldOffset(0)]
        public byte BeastGaugeAmount;
    }
}

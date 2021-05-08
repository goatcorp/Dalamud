using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory RDM job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RDMGauge
    {
        [FieldOffset(0)]
        public byte WhiteGauge;

        [FieldOffset(1)]
        public byte BlackGauge;
    }
}

using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory WAR job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WARGauge
    {
        /// <summary>
        /// Gets the amount of wrath in the Beast gauge.
        /// </summary>
        [FieldOffset(0)]
        public byte BeastGaugeAmount;
    }
}

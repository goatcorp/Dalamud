using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory PLD job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct PLDGauge
    {
        /// <summary>
        /// Gets the current level of the Oath gauge.
        /// </summary>
        [FieldOffset(0)]
        public byte GaugeAmount;
    }
}

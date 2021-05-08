using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs.JobGauge
{
    /// <summary>
    /// In-memory GNB job gauge.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct GNBGauge
    {
        [FieldOffset(0)]
        public byte NumAmmo;

        [FieldOffset(2)]
        public short MaxTimerDuration;

        [FieldOffset(4)]
        public byte AmmoComboStepNumber;
    }
}

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory GNB job gauge.
/// </summary>
public unsafe class GNBGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.GunbreakerGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GNBGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal GNBGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the amount of ammo available.
    /// </summary>
    public byte Ammo => this.Struct->Ammo;

    /// <summary>
    /// Gets the max combo time of the Gnashing Fang combo.
    /// </summary>
    public short MaxTimerDuration => this.Struct->MaxTimerDuration;

    /// <summary>
    /// Gets the current step of the Gnashing Fang combo.
    /// </summary>
    public byte AmmoComboStep => this.Struct->AmmoComboStep;
}

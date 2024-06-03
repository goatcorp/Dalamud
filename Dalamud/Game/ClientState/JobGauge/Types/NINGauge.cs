namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory NIN job gauge.
/// </summary>
public unsafe class NINGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.NinjaGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NINGauge"/> class.
    /// </summary>
    /// <param name="address">The address of the gauge.</param>
    internal NINGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the time left on Huton in milliseconds.
    /// </summary>
    public int HutonTimer => this.Struct->HutonTimer;

    /// <summary>
    /// Gets the amount of Ninki available.
    /// </summary>
    public byte Ninki => this.Struct->Ninki;

    /// <summary>
    /// Gets the number of times Huton has been cast manually.
    /// </summary>
    public byte HutonManualCasts => this.Struct->HutonManualCasts;
}

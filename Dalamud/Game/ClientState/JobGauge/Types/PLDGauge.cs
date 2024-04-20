namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory PLD job gauge.
/// </summary>
public unsafe class PLDGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.PaladinGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PLDGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal PLDGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the current level of the Oath gauge.
    /// </summary>
    public byte OathGauge => this.Struct->OathGauge;
}

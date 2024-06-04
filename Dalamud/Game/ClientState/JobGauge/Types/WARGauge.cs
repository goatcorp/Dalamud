namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory WAR job gauge.
/// </summary>
public unsafe class WARGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.WarriorGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WARGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal WARGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the amount of wrath in the Beast gauge.
    /// </summary>
    public byte BeastGauge => this.Struct->BeastGauge;
}

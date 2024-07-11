namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory WHM job gauge.
/// </summary>
public unsafe class WHMGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.WhiteMageGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WHMGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal WHMGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the time to next lily in milliseconds.
    /// </summary>
    public short LilyTimer => this.Struct->LilyTimer;

    /// <summary>
    /// Gets the number of Lilies.
    /// </summary>
    public byte Lily => this.Struct->Lily;

    /// <summary>
    /// Gets the number of times the blood lily has been nourished.
    /// </summary>
    public byte BloodLily => this.Struct->BloodLily;
}

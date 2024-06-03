namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory DRG job gauge.
/// </summary>
public unsafe class DRGGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.DragoonGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DRGGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal DRGGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the time remaining for Life of the Dragon in milliseconds.
    /// </summary>
    public short LOTDTimer => this.Struct->LotdTimer;

    /// <summary>
    /// Gets a value indicating whether Life of the Dragon is active.
    /// </summary>
    public bool IsLOTDActive => this.Struct->LotdState == 2;

    /// <summary>
    /// Gets the count of eyes opened during Blood of the Dragon.
    /// </summary>
    public byte EyeCount => this.Struct->EyeCount;

    /// <summary>
    /// Gets the amount of Firstminds' Focus available.
    /// </summary>
    public byte FirstmindsFocusCount => this.Struct->FirstmindsFocusCount;
}

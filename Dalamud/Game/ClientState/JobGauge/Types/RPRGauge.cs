namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory RPR job gauge.
/// </summary>
public unsafe class RPRGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.ReaperGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RPRGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal RPRGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the amount of Soul available.
    /// </summary>
    public byte Soul => this.Struct->Soul;

    /// <summary>
    /// Gets the amount of Shroud available.
    /// </summary>
    public byte Shroud => this.Struct->Shroud;

    /// <summary>
    /// Gets the time remaining that Enshrouded is active.
    /// </summary>
    public ushort EnshroudedTimeRemaining => this.Struct->EnshroudedTimeRemaining;

    /// <summary>
    /// Gets the amount of Lemure Shroud available.
    /// </summary>
    public byte LemureShroud => this.Struct->LemureShroud;

    /// <summary>
    /// Gets the amount of Void Shroud available.
    /// </summary>
    public byte VoidShroud => this.Struct->VoidShroud;
}

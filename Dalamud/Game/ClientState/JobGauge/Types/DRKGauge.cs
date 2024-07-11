namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory DRK job gauge.
/// </summary>
public unsafe class DRKGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.DarkKnightGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DRKGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal DRKGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the amount of blood accumulated.
    /// </summary>
    public byte Blood => this.Struct->Blood;

    /// <summary>
    /// Gets the Darkside time remaining in milliseconds.
    /// </summary>
    public ushort DarksideTimeRemaining => this.Struct->DarksideTimer;

    /// <summary>
    /// Gets the Shadow time remaining in milliseconds.
    /// </summary>
    public ushort ShadowTimeRemaining => this.Struct->ShadowTimer;

    /// <summary>
    /// Gets a value indicating whether the player has Dark Arts or not.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool HasDarkArts => this.Struct->DarkArtsState > 0;
}

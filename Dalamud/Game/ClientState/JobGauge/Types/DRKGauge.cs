using Dalamud.Game.ClientState.JobGauge.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory DRK job gauge.
/// </summary>
public unsafe class DRKGauge : JobGaugeBase<DarkKnightGauge>
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

    /// <summary>
    /// Gets the step of the Delirium Combo (Scarlet Delirium, Comeuppance,
    /// Torcleaver) that the player is on.<br/>
    /// Does not in any way consider whether the player is still under Delirium, or
    /// if the player still has stacks of Delirium to use.
    /// </summary>
    /// <remarks>
    /// Value will persist until combo is finished OR
    /// if the combo is not completed then the value will stay until about halfway into Delirium's cooldown.
    /// </remarks>
    public DeliriumStep DeliriumComboStep => (DeliriumStep)this.Struct->DeliriumStep;
}

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory RDM job gauge.
/// </summary>
public unsafe class RDMGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.RedMageGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RDMGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal RDMGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the level of the White gauge.
    /// </summary>
    public byte WhiteMana => this.Struct->WhiteMana;

    /// <summary>
    /// Gets the level of the Black gauge.
    /// </summary>
    public byte BlackMana => this.Struct->BlackMana;

    /// <summary>
    /// Gets the amount of mana stacks.
    /// </summary>
    public byte ManaStacks => this.Struct->ManaStacks;
}

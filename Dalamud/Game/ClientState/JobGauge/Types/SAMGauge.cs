using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory SAM job gauge.
/// </summary>
public unsafe class SAMGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.SamuraiGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SAMGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal SAMGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the currently active Kaeshi ability.
    /// </summary>
    public Kaeshi Kaeshi => (Kaeshi)this.Struct->Kaeshi;

    /// <summary>
    /// Gets the current amount of Kenki available.
    /// </summary>
    public byte Kenki => this.Struct->Kenki;

    /// <summary>
    /// Gets the amount of Meditation stacks.
    /// </summary>
    public byte MeditationStacks => this.Struct->MeditationStacks;

    /// <summary>
    /// Gets the active Sen.
    /// </summary>
    public Sen Sen => (Sen)this.Struct->SenFlags;

    /// <summary>
    /// Gets a value indicating whether the Setsu Sen is active.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool HasSetsu => (this.Sen & Sen.SETSU) != 0;

    /// <summary>
    /// Gets a value indicating whether the Getsu Sen is active.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool HasGetsu => (this.Sen & Sen.GETSU) != 0;

    /// <summary>
    /// Gets a value indicating whether the Ka Sen is active.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool HasKa => (this.Sen & Sen.KA) != 0;
}

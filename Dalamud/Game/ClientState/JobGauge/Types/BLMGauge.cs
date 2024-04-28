namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory BLM job gauge.
/// </summary>
public unsafe class BLMGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.BlackMageGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BLMGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal BLMGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the time remaining for the Enochian time in milliseconds.
    /// </summary>
    public short EnochianTimer => this.Struct->EnochianTimer;

    /// <summary>
    /// Gets the time remaining for Astral Fire or Umbral Ice in milliseconds.
    /// </summary>
    public short ElementTimeRemaining => this.Struct->ElementTimeRemaining;

    /// <summary>
    /// Gets the number of Polyglot stacks remaining.
    /// </summary>
    public byte PolyglotStacks => this.Struct->PolyglotStacks;

    /// <summary>
    /// Gets the number of Umbral Hearts remaining.
    /// </summary>
    public byte UmbralHearts => this.Struct->UmbralHearts;

    /// <summary>
    /// Gets the amount of Umbral Ice stacks.
    /// </summary>
    public byte UmbralIceStacks => (byte)(this.InUmbralIce ? -this.Struct->ElementStance : 0);

    /// <summary>
    /// Gets the amount of Astral Fire stacks.
    /// </summary>
    public byte AstralFireStacks => (byte)(this.InAstralFire ? this.Struct->ElementStance : 0);

    /// <summary>
    /// Gets a value indicating whether or not the player is in Umbral Ice.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool InUmbralIce => this.Struct->ElementStance < 0;

    /// <summary>
    /// Gets a value indicating whether or not the player is in Astral fire.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool InAstralFire => this.Struct->ElementStance > 0;

    /// <summary>
    /// Gets a value indicating whether or not Enochian is active.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsEnochianActive => this.Struct->EnochianActive;

    /// <summary>
    /// Gets a value indicating whether Paradox is active.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsParadoxActive => this.Struct->ParadoxActive;
}

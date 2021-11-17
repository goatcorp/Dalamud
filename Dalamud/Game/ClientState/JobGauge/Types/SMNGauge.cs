using System;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory SMN job gauge.
/// </summary>
public unsafe class SMNGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.SummonerGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SMNGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal SMNGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the time remaining for the current summon.
    /// </summary>
    public short TimerRemaining => this.Struct->TimerRemaining;

    /// <summary>
    /// Gets the summon that will return after the current summon expires.
    /// </summary>
    public SummonPet ReturnSummon => (SummonPet)this.Struct->ReturnSummon;

    /// <summary>
    /// Gets the summon glam for the <see cref="ReturnSummon"/>.
    /// </summary>
    public PetGlam ReturnSummonGlam => (PetGlam)this.Struct->ReturnSummonGlam;

    /// <summary>
    /// Gets the current aether flags.
    /// Use the summon accessors instead.
    /// </summary>
    public byte AetherFlags => this.Struct->AetherFlags;

    /// <summary>
    /// Gets a value indicating whether if Phoenix is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsPhoenixReady => (this.AetherFlags & 0x10) > 0;

    /// <summary>
    /// Gets a value indicating whether Bahamut is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsBahamutReady => (this.AetherFlags & 8) > 0;

    /// <summary>
    /// Gets a value indicating whether there are any Aetherflow stacks available.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool HasAetherflowStacks => (this.AetherFlags & 3) > 0;
}

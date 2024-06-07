using Dalamud.Game.ClientState.JobGauge.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory SMN job gauge.
/// </summary>
public unsafe class SMNGauge : JobGaugeBase<SummonerGauge>
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
    public ushort SummonTimerRemaining => this.Struct->SummonTimer;

    /// <summary>
    /// Gets the time remaining for the current attunement.
    /// </summary>
    public ushort AttunmentTimerRemaining => this.Struct->AttunementTimer;

    /// <summary>
    /// Gets the summon that will return after the current summon expires.
    /// This maps to the <see cref="Lumina.Excel.GeneratedSheets.Pet"/> sheet.
    /// </summary>
    public SummonPet ReturnSummon => (SummonPet)this.Struct->ReturnSummon;

    /// <summary>
    /// Gets the summon glam for the <see cref="ReturnSummon"/>.
    /// This maps to the <see cref="Lumina.Excel.GeneratedSheets.PetMirage"/> sheet.
    /// </summary>
    public PetGlam ReturnSummonGlam => (PetGlam)this.Struct->ReturnSummonGlam;

    /// <summary>
    /// Gets the amount of aspected Attunment remaining.
    /// </summary>
    public byte Attunement => this.Struct->Attunement;

    /// <summary>
    /// Gets the current aether flags.
    /// Use the summon accessors instead.
    /// </summary>
    public AetherFlags AetherFlags => this.Struct->AetherFlags;

    /// <summary>
    /// Gets a value indicating whether Bahamut is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsBahamutReady => !this.AetherFlags.HasFlag(AetherFlags.PhoenixReady);

    /// <summary>
    /// Gets a value indicating whether if Phoenix is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsPhoenixReady => this.AetherFlags.HasFlag(AetherFlags.PhoenixReady);

    /// <summary>
    /// Gets a value indicating whether if Ifrit is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsIfritReady => this.AetherFlags.HasFlag(AetherFlags.IfritReady);

    /// <summary>
    /// Gets a value indicating whether if Titan is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsTitanReady => this.AetherFlags.HasFlag(AetherFlags.TitanReady);

    /// <summary>
    /// Gets a value indicating whether if Garuda is ready to be summoned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsGarudaReady => this.AetherFlags.HasFlag(AetherFlags.GarudaReady);

    /// <summary>
    /// Gets a value indicating whether if Ifrit is currently attuned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsIfritAttuned => this.AetherFlags.HasFlag(AetherFlags.IfritAttuned) && !this.AetherFlags.HasFlag(AetherFlags.GarudaAttuned);

    /// <summary>
    /// Gets a value indicating whether if Titan is currently attuned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsTitanAttuned => this.AetherFlags.HasFlag(AetherFlags.TitanAttuned) && !this.AetherFlags.HasFlag(AetherFlags.GarudaAttuned);

    /// <summary>
    /// Gets a value indicating whether if Garuda is currently attuned.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsGarudaAttuned => this.AetherFlags.HasFlag(AetherFlags.GarudaAttuned);

    /// <summary>
    /// Gets a value indicating whether there are any Aetherflow stacks available.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool HasAetherflowStacks => this.AetherflowStacks > 0;

    /// <summary>
    /// Gets the amount of Aetherflow available.
    /// </summary>
    public byte AetherflowStacks => (byte)(this.AetherFlags & AetherFlags.Aetherflow);
}

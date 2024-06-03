using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// This class represents the battle characters.
/// </summary>
public unsafe class BattleChara : Character
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BattleChara"/> class.
    /// This represents a battle character.
    /// </summary>
    /// <param name="address">The address of this character in memory.</param>
    internal BattleChara(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the current status effects.
    /// </summary>
    public StatusList StatusList => new(this.Struct->GetStatusManager);

    /// <summary>
    /// Gets a value indicating whether the chara is currently casting.
    /// </summary>
    public bool IsCasting => this.Struct->GetCastInfo->IsCasting > 0;

    /// <summary>
    /// Gets a value indicating whether the cast is interruptible.
    /// </summary>
    public bool IsCastInterruptible => this.Struct->GetCastInfo->Interruptible > 0;

    /// <summary>
    /// Gets the spell action type of the spell being cast by the actor.
    /// </summary>
    public byte CastActionType => (byte)this.Struct->GetCastInfo->ActionType;

    /// <summary>
    /// Gets the spell action ID of the spell being cast by the actor.
    /// </summary>
    public uint CastActionId => this.Struct->GetCastInfo->ActionID;

    /// <summary>
    /// Gets the object ID of the target currently being cast at by the chara.
    /// </summary>
    public uint CastTargetObjectId => this.Struct->GetCastInfo->CastTargetID;

    /// <summary>
    /// Gets the current casting time of the spell being cast by the chara.
    /// </summary>
    public float CurrentCastTime => this.Struct->GetCastInfo->CurrentCastTime;

    /// <summary>
    /// Gets the total casting time of the spell being cast by the chara.
    /// </summary>
    /// <remarks>
    /// This can only be a portion of the total cast for some actions.
    /// Use AdjustedTotalCastTime if you always need the total cast time.
    /// </remarks>
    [Api10ToDo("Rename so it is not confused with AdjustedTotalCastTime")]
    public float TotalCastTime => this.Struct->GetCastInfo->TotalCastTime;

    /// <summary>
    /// Gets the <see cref="TotalCastTime"/> plus any adjustments from the game, such as Action offset 2B. Used for display purposes.
    /// </summary>
    /// <remarks>
    /// This is the actual total cast time for all actions.
    /// </remarks>
    [Api10ToDo("Rename so it is not confused with TotalCastTime")]
    public float AdjustedTotalCastTime => this.Struct->GetCastInfo->AdjustedTotalCastTime;

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)this.Address;
}

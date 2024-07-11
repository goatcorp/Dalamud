using Dalamud.Game.ClientState.Statuses;
using Dalamud.Utility;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// Interface representing a battle character.
/// </summary>
public interface IBattleChara : ICharacter
{
    /// <summary>
    /// Gets the current status effects.
    /// </summary>
    public StatusList StatusList { get; }

    /// <summary>
    /// Gets a value indicating whether the chara is currently casting.
    /// </summary>
    public bool IsCasting { get; }

    /// <summary>
    /// Gets a value indicating whether the cast is interruptible.
    /// </summary>
    public bool IsCastInterruptible { get; }

    /// <summary>
    /// Gets the spell action type of the spell being cast by the actor.
    /// </summary>
    public byte CastActionType { get; }

    /// <summary>
    /// Gets the spell action ID of the spell being cast by the actor.
    /// </summary>
    public uint CastActionId { get; }

    /// <summary>
    /// Gets the object ID of the target currently being cast at by the chara.
    /// </summary>
    public ulong CastTargetObjectId { get; }

    /// <summary>
    /// Gets the current casting time of the spell being cast by the chara.
    /// </summary>
    public float CurrentCastTime { get; }

    /// <summary>
    /// Gets the base casting time of the spell being cast by the chara.
    /// </summary>
    /// <remarks>
    /// This can only be a portion of the total cast for some actions.
    /// Use TotalCastTime if you always need the total cast time.
    /// </remarks>
    public float BaseCastTime { get; }

    /// <summary>
    /// Gets the <see cref="BaseCastTime"/> plus any adjustments from the game, such as Action offset 2B. Used for display purposes.
    /// </summary>
    public float TotalCastTime { get; }
}

/// <summary>
/// This class represents the battle characters.
/// </summary>
internal unsafe class BattleChara : Character, IBattleChara
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

    /// <inheritdoc/>
    public StatusList StatusList => new(this.Struct->GetStatusManager());

    /// <inheritdoc/>
    public bool IsCasting => this.Struct->GetCastInfo()->IsCasting > 0;

    /// <inheritdoc/>
    public bool IsCastInterruptible => this.Struct->GetCastInfo()->Interruptible > 0;

    /// <inheritdoc/>
    public byte CastActionType => (byte)this.Struct->GetCastInfo()->ActionType;

    /// <inheritdoc/>
    public uint CastActionId => this.Struct->GetCastInfo()->ActionId;

    /// <inheritdoc/>
    public ulong CastTargetObjectId => this.Struct->GetCastInfo()->TargetId;

    /// <inheritdoc/>
    public float CurrentCastTime => this.Struct->GetCastInfo()->CurrentCastTime;

    /// <inheritdoc/>
    public float BaseCastTime => this.Struct->GetCastInfo()->BaseCastTime;

    /// <inheritdoc/>
    public float TotalCastTime => this.Struct->GetCastInfo()->TotalCastTime;

    /// <summary>
    /// Gets the underlying structure.
    /// </summary>
    protected internal new FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)this.Address;
}

using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

public interface IPlayerCharacter : ICharacter
{
    /// <summary>
    /// Gets the current <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    unsafe ExcelResolver<Lumina.Excel.GeneratedSheets.World> CurrentWorld { get; }

    /// <summary>
    /// Gets the home <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    unsafe ExcelResolver<Lumina.Excel.GeneratedSheets.World> HomeWorld { get; }

    /// <summary>
    /// Gets the target actor ID of the PlayerCharacter.
    /// </summary>
    unsafe ulong TargetObjectId { get; }

    /// <summary>
    /// Gets the name of this <see cref="GameObject" />.
    /// </summary>
    unsafe SeString Name { get; }

    /// <summary>
    /// Gets the object ID of this <see cref="GameObject" />.
    /// </summary>
    unsafe uint ObjectId { get; }

    /// <summary>
    /// Gets the data ID for linking to other respective game data.
    /// </summary>
    unsafe uint DataId { get; }

    /// <summary>
    /// Gets the ID of this GameObject's owner.
    /// </summary>
    unsafe uint OwnerId { get; }

    /// <summary>
    /// Gets the index of this object in the object table.
    /// </summary>
    unsafe ushort ObjectIndex { get; }

    /// <summary>
    /// Gets the entity kind of this <see cref="GameObject" />.
    /// See <see cref="ObjectKind">the ObjectKind enum</see> for possible values.
    /// </summary>
    unsafe ObjectKind ObjectKind { get; }

    /// <summary>
    /// Gets the sub kind of this Actor.
    /// </summary>
    unsafe byte SubKind { get; }

    /// <summary>
    /// Gets the X distance from the local player in yalms.
    /// </summary>
    unsafe byte YalmDistanceX { get; }

    /// <summary>
    /// Gets the Y distance from the local player in yalms.
    /// </summary>
    unsafe byte YalmDistanceZ { get; }

    /// <summary>
    /// Gets a value indicating whether the object is dead or alive.
    /// </summary>
    unsafe bool IsDead { get; }

    /// <summary>
    /// Gets a value indicating whether the object is targetable.
    /// </summary>
    unsafe bool IsTargetable { get; }

    /// <summary>
    /// Gets the position of this <see cref="GameObject" />.
    /// </summary>
    unsafe Vector3 Position { get; }

    /// <summary>
    /// Gets the rotation of this <see cref="GameObject" />.
    /// This ranges from -pi to pi radians.
    /// </summary>
    unsafe float Rotation { get; }

    /// <summary>
    /// Gets the hitbox radius of this <see cref="GameObject" />.
    /// </summary>
    unsafe float HitboxRadius { get; }

    /// <summary>
    /// Gets the target object of the game object.
    /// </summary>
    /// <remarks>
    /// This iterates the actor table, it should be used with care.
    /// </remarks>
// TODO: Fix for non-networked GameObjects
IGameObject? TargetObject { get; }

    /// <summary>
    /// Gets the address of the game object in memory.
    /// </summary>
    IntPtr Address { get; }

    /// <summary>
    /// Gets the current HP of this Chara.
    /// </summary>
    unsafe uint CurrentHp { get; }

    /// <summary>
    /// Gets the maximum HP of this Chara.
    /// </summary>
    unsafe uint MaxHp { get; }

    /// <summary>
    /// Gets the current MP of this Chara.
    /// </summary>
    unsafe uint CurrentMp { get; }

    /// <summary>
    /// Gets the maximum MP of this Chara.
    /// </summary>
    unsafe uint MaxMp { get; }

    /// <summary>
    /// Gets the current GP of this Chara.
    /// </summary>
    unsafe uint CurrentGp { get; }

    /// <summary>
    /// Gets the maximum GP of this Chara.
    /// </summary>
    unsafe uint MaxGp { get; }

    /// <summary>
    /// Gets the current CP of this Chara.
    /// </summary>
    unsafe uint CurrentCp { get; }

    /// <summary>
    /// Gets the maximum CP of this Chara.
    /// </summary>
    unsafe uint MaxCp { get; }

    /// <summary>
    /// Gets the shield percentage of this Chara.
    /// </summary>
    unsafe byte ShieldPercentage { get; }

    /// <summary>
    /// Gets the ClassJob of this Chara.
    /// </summary>
    unsafe ExcelResolver<ClassJob> ClassJob { get; }

    /// <summary>
    /// Gets the level of this Chara.
    /// </summary>
    unsafe byte Level { get; }

    /// <summary>
    /// Gets a byte array describing the visual appearance of this Chara.
    /// Indexed by <see cref="CustomizeIndex"/>.
    /// </summary>
    unsafe byte[] Customize { get; }

    /// <summary>
    /// Gets the Free Company tag of this chara.
    /// </summary>
    unsafe SeString CompanyTag { get; }

    /// <summary>
    /// Gets the name ID of the character.
    /// </summary>
    unsafe uint NameId { get; }

    /// <summary>
    /// Gets the current online status of the character.
    /// </summary>
    unsafe ExcelResolver<OnlineStatus> OnlineStatus { get; }

    /// <summary>
    /// Gets the status flags.
    /// </summary>
    unsafe StatusFlags StatusFlags { get; }

    /// <summary>
    /// Gets the current status effects.
    /// </summary>
    unsafe StatusList StatusList { get; }

    /// <summary>
    /// Gets a value indicating whether the chara is currently casting.
    /// </summary>
    unsafe bool IsCasting { get; }

    /// <summary>
    /// Gets a value indicating whether the cast is interruptible.
    /// </summary>
    unsafe bool IsCastInterruptible { get; }

    /// <summary>
    /// Gets the spell action type of the spell being cast by the actor.
    /// </summary>
    unsafe byte CastActionType { get; }

    /// <summary>
    /// Gets the spell action ID of the spell being cast by the actor.
    /// </summary>
    unsafe uint CastActionId { get; }

    /// <summary>
    /// Gets the object ID of the target currently being cast at by the chara.
    /// </summary>
    unsafe uint CastTargetObjectId { get; }

    /// <summary>
    /// Gets the current casting time of the spell being cast by the chara.
    /// </summary>
    unsafe float CurrentCastTime { get; }

    /// <summary>
    /// Gets the total casting time of the spell being cast by the chara.
    /// </summary>
    /// <remarks>
    /// This can only be a portion of the total cast for some actions.
    /// Use AdjustedTotalCastTime if you always need the total cast time.
    /// </remarks>
    unsafe float TotalCastTime { get; }

    /// <summary>
    /// Gets the <see cref="TotalCastTime"/> plus any adjustments from the game, such as Action offset 2B. Used for display purposes.
    /// </summary>
    /// <remarks>
    /// This is the actual total cast time for all actions.
    /// </remarks>
    unsafe float AdjustedTotalCastTime { get; }

    /// <inheritdoc/>
    bool Equals(object obj);

    /// <inheritdoc/>
    int GetHashCode();

    /// <inheritdoc/>
    string ToString();

    /// <summary>
    /// Gets a value indicating whether this actor is still valid in memory.
    /// </summary>
    /// <returns>True or false.</returns>
    bool IsValid();
}

/// <summary>
/// This class represents a player character.
/// </summary>
public unsafe class PlayerCharacter : BattleChara, IPlayerCharacter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerCharacter"/> class.
    /// This represents a player character.
    /// </summary>
    /// <param name="address">The address of this actor in memory.</param>
    internal PlayerCharacter(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the current <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.World> CurrentWorld => new(this.Struct->CurrentWorld);

    /// <summary>
    /// Gets the home <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.World> HomeWorld => new(this.Struct->HomeWorld);

    /// <summary>
    /// Gets the target actor ID of the PlayerCharacter.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->LookAt.Controller.Params[0].TargetParam.TargetId;
}

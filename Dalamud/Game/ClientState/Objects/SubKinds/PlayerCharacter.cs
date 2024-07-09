using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Game.Text.SeStringHandling;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

/// <summary>
/// Interface representing a player character.
/// </summary>
public interface IPlayerCharacter : IBattleChara
{
    /// <summary>
    /// Gets the current <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    ExcelResolver<World> CurrentWorld { get; }

    /// <summary>
    /// Gets the home <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    ExcelResolver<World> HomeWorld { get; }
}

/// <summary>
/// This class represents a player character.
/// </summary>
internal unsafe class PlayerCharacter : BattleChara, IPlayerCharacter
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

    /// <inheritdoc/>
    public ExcelResolver<World> CurrentWorld => new(this.Struct->CurrentWorld);

    /// <inheritdoc/>
    public ExcelResolver<World> HomeWorld => new(this.Struct->HomeWorld);

    /// <summary>
    /// Gets the target actor ID of the PlayerCharacter.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->LookAt.Controller.Params[0].TargetParam.TargetId;
}

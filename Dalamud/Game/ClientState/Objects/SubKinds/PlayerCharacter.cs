using Dalamud.Data;
using Dalamud.Game.ClientState.Objects.Types;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

/// <summary>
/// Interface representing a player character.
/// </summary>
public interface IPlayerCharacter : IBattleChara
{
    /// <summary>
    /// Gets the current <see cref="RowRef{T}">world</see> of the character.
    /// </summary>
    RowRef<World> CurrentWorld { get; }

    /// <summary>
    /// Gets the home <see cref="RowRef{T}">world</see> of the character.
    /// </summary>
    RowRef<World> HomeWorld { get; }
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
    public RowRef<World> CurrentWorld => LuminaUtils.CreateRef<World>(this.Struct->CurrentWorld);

    /// <inheritdoc/>
    public RowRef<World> HomeWorld => LuminaUtils.CreateRef<World>(this.Struct->HomeWorld);

    /// <summary>
    /// Gets the target actor ID of the PlayerCharacter.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->LookAt.Controller.Params[0].TargetParam.TargetId;
}

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

/// <summary>
/// This class represents a player character.
/// </summary>
public unsafe class PlayerCharacter : BattleChara
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
    public ExcelResolver<Lumina.Excel.GeneratedSheets.World> CurrentWorld => new(this.Struct->Character.CurrentWorld);

    /// <summary>
    /// Gets the home <see cref="ExcelResolver{T}">world</see> of the character.
    /// </summary>
    public ExcelResolver<Lumina.Excel.GeneratedSheets.World> HomeWorld => new(this.Struct->Character.HomeWorld);

    /// <summary>
    /// Gets the target actor ID of the PlayerCharacter.
    /// </summary>
    public override ulong TargetObjectId => this.Struct->Character.LookTargetId;
}

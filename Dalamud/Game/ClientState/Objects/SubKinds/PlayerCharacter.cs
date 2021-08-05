using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Resolvers;

namespace Dalamud.Game.ClientState.Objects.SubKinds
{
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
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        internal PlayerCharacter(IntPtr address, Dalamud dalamud)
            : base(address, dalamud)
        {
        }

        /// <summary>
        /// Gets the current <see cref="ExcelResolver{T}">world</see> of the character.
        /// </summary>
        public ExcelResolver<Lumina.Excel.GeneratedSheets.World> CurrentWorld => new(this.Struct->Character.CurrentWorld, this.Dalamud);

        /// <summary>
        /// Gets the home <see cref="ExcelResolver{T}">world</see> of the character.
        /// </summary>
        public ExcelResolver<Lumina.Excel.GeneratedSheets.World> HomeWorld => new(this.Struct->Character.HomeWorld, this.Dalamud);

        /// <summary>
        /// Gets the target actor ID of the PlayerCharacter.
        /// </summary>
        public override uint TargetObjectId => this.Struct->Character.GameObject.TargetObjectID;
    }
}

using System;
using Dalamud.Game.ClientState.Actors.Resolvers;
using SharpDX.Text;

namespace Dalamud.Game.ClientState.Actors.Types {
    /// <summary>
    ///     This class represents a player character.
    /// </summary>
    public class PlayerCharacter : Chara {
        /// <summary>
        ///     Set up a new player character with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        public PlayerCharacter(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud) : base(address, actorStruct, dalamud) { }

        /// <summary>
        ///     The current <see cref="World">world</see> of the character.
        /// </summary>
        public World CurrentWorld => new World(this.actorStruct.CurrentWorld, this.dalamud);

        /// <summary>
        ///     The home <see cref="World">world</see> of the character.
        /// </summary>
        public World HomeWorld => new World(this.actorStruct.HomeWorld, this.dalamud);

        /// <summary>
        ///     The Free Company tag of this player.
        /// </summary>
        public string CompanyTag => Encoding.UTF8.GetString(this.actorStruct.CompanyTag).Substring(2).Replace("\0", "");

        /// <summary>
        /// Target of the PlayerCharacter
        /// </summary>
        public override int TargetActorID => this.actorStruct.PlayerCharacterTargetActorId;

    }
}

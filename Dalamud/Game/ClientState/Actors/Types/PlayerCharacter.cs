using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Resolvers;
using Dalamud.Game.ClientState.Structs;
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
        public string CompanyTag {
            get {
                var bytes = new byte[6];
                Marshal.Copy(this.Address + ActorOffsets.CompanyTag, bytes, 0, bytes.Length);

                return Encoding.UTF8.GetString(bytes);
            }
        }

        /// <summary>
        /// Target of the PlayerCharacter
        /// </summary>
        public override int TargetActorID => this.actorStruct.PlayerCharacterTargetActorId;

    }
}

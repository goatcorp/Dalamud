using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Game.ClientState.Actors.Resolvers;
using Dalamud.Game.ClientState.Structs;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// This class represents a player character.
    /// </summary>
    public class PlayerCharacter : Chara
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerCharacter"/> class.
        /// This represents a player character.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        internal PlayerCharacter(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud)
            : base(address, actorStruct, dalamud)
        {
            var companyTagBytes = new byte[5];
            Marshal.Copy(this.Address + ActorOffsets.CompanyTag, companyTagBytes, 0, companyTagBytes.Length);
            this.CompanyTag = Encoding.UTF8.GetString(companyTagBytes.TakeWhile(c => c != 0x0).ToArray());
        }

        /// <summary>
        /// Gets the current <see cref="World">world</see> of the character.
        /// </summary>
        public World CurrentWorld => new(this.ActorStruct.CurrentWorld, this.Dalamud);

        /// <summary>
        /// Gets the home <see cref="World">world</see> of the character.
        /// </summary>
        public World HomeWorld => new(this.ActorStruct.HomeWorld, this.Dalamud);

        /// <summary>
        /// Gets the Free Company tag of this player.
        /// </summary>
        public string CompanyTag { get; private set; }

        /// <summary>
        /// Gets the target of the PlayerCharacter.
        /// </summary>
        public override int TargetActorID => this.ActorStruct.PlayerCharacterTargetActorId;
    }
}

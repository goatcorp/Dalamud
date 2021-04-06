using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Actors.Resolvers;
using Dalamud.Game.ClientState.Structs;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    ///     This class represents a player character.
    /// </summary>
    public unsafe class PlayerCharacter : Chara
    {
        /// <summary>
        ///     Set up a new player character with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        public PlayerCharacter(IntPtr address, Dalamud dalamud) : base(address, dalamud)
        {
        }

        /// <summary>
        ///     The current <see cref="World">world</see> of the character.
        /// </summary>
        public World CurrentWorld => new World(*(ushort*)(Address + ActorOffsets.CurrentWorld), this.dalamud);

        /// <summary>
        ///     The home <see cref="World">world</see> of the character.
        /// </summary>
        public World HomeWorld => new World(*(ushort*)(Address + ActorOffsets.HomeWorld), this.dalamud);

        /// <summary>
        ///     The Free Company tag of this player.
        /// </summary>
        public string CompanyTag
        {
            get
            {
                var fcTagBytes = new byte[5];
                Marshal.Copy(this.Address + ActorOffsets.CompanyTag, fcTagBytes, 0, fcTagBytes.Length);

                return Encoding.UTF8.GetString( fcTagBytes.TakeWhile( x => x != 0x00 ).ToArray() );
            }
        }

        /// <summary>
        /// Target of the PlayerCharacter.
        /// </summary>
        public override int TargetActorID => *(int*)(Address + ActorOffsets.PlayerCharacterTargetActorId);

    }
}

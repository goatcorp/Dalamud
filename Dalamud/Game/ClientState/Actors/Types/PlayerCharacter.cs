using System;

using Dalamud.Game.ClientState.Resolvers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// This class represents a player character.
    /// </summary>
    public unsafe class PlayerCharacter : Chara
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
        /// Gets the current <see cref="WorldResolver">world</see> of the character.
        /// </summary>
        public WorldResolver CurrentWorld => new(*(ushort*)(this.Address + ActorOffsets.CurrentWorld), this.Dalamud);

        /// <summary>
        /// Gets the home <see cref="WorldResolver">world</see> of the character.
        /// </summary>
        public WorldResolver HomeWorld => new(*(ushort*)(this.Address + ActorOffsets.HomeWorld), this.Dalamud);

        /// <summary>
        /// Gets the Free Company tag of this player.
        /// </summary>
        public SeString CompanyTag => MemoryHelper.ReadSeString(this.Address + ActorOffsets.CompanyTag, 6);

        /// <summary>
        /// Gets the target actor ID of the PlayerCharacter.
        /// </summary>
        public override uint TargetActorID => *(uint*)(this.Address + ActorOffsets.PlayerCharacterTargetActorId);
    }
}

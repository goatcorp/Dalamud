using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    /// This class represents a NPC.
    /// </summary>
    public unsafe class Npc : Chara
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Npc"/> class.
        /// Set up a new NPC with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        internal Npc(IntPtr address, Dalamud dalamud)
            : base(address, dalamud)
        {
        }

        /// <summary>
        /// Gets the data ID of the NPC linking to their assoicated BNpcBase data.
        /// </summary>
        public uint BaseId => *(uint*)(this.Address + ActorOffsets.DataId);

        /// <summary>
        /// Gets the name ID of the NPC linking to their respective game data.
        /// </summary>
        public uint NameId => *(uint*)(this.Address + ActorOffsets.NameId);
    }
}

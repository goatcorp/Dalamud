using System;

using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.ClientState.Objects.SubKinds
{
    /// <summary>
    /// This class represents a NPC.
    /// </summary>
    public unsafe class Npc : Character
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
        /// Gets the name ID of the NPC linking to their respective game data.
        /// </summary>
        public uint NameId => this.Struct->NameID;
    }
}

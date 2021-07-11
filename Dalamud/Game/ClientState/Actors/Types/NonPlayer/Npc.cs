using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    /// This class represents a NPC.
    /// </summary>
    public class Npc : Chara
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Npc"/> class.
        /// This represents a Non-playable Character.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        internal Npc(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud)
            : base(address, actorStruct, dalamud)
        {
        }

        /// <summary>
        /// Gets the data ID of the NPC linking to their respective game data.
        /// </summary>
        public int DataId => this.ActorStruct.DataId;

        /// <summary>
        /// Gets the name ID of the NPC linking to their respective game data.
        /// </summary>
        public int NameId => this.ActorStruct.NameId;
    }
}

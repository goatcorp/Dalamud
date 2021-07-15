using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    /// This class represents an EventObj.
    /// </summary>
    public class EventObj : Actor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventObj"/> class.
        /// This represents an Event Object.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        internal EventObj(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud)
            : base(address, actorStruct, dalamud)
        {
        }

        /// <summary>
        /// Gets the data ID of the NPC linking to their respective game data.
        /// </summary>
        public int DataId => this.ActorStruct.DataId;
    }
}

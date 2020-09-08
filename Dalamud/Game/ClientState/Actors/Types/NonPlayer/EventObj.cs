using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer {
    /// <summary>
    ///     This class represents an EventObj.
    /// </summary>
    public class EventObj : Actor {
        /// <summary>
        ///     Set up a new EventObj with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        public EventObj(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud) : base(address, actorStruct, dalamud) { }

        /// <summary>
        ///     The data ID of the NPC linking to their respective game data.
        /// </summary>
        public int DataId => this.actorStruct.DataId;
    }
}

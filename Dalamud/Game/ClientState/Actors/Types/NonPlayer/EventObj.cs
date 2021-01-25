using System;
using Dalamud.Game.ClientState.Structs;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer {
    /// <summary>
    ///     This class represents an EventObj.
    /// </summary>
    public unsafe class EventObj : Actor {
        /// <summary>
        ///     Set up a new EventObj with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        public EventObj( IntPtr address, Dalamud dalamud ) : base(address, dalamud) { }

        /// <summary>
        ///     The data ID of the NPC linking to their respective game data.
        /// </summary>
        public int EventObjectId => *(int*)(Address + ActorOffsets.DataId);
    }
}

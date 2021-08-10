using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    /// This class represents an EventObj.
    /// </summary>
    public unsafe class EventObj : Actor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventObj"/> class.
        /// Set up a new EventObj with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        internal EventObj(IntPtr address)
            : base(address)
        {
        }

        /// <summary>
        /// Gets the event object ID of the linking to their respective game data.
        /// </summary>
        public uint EventObjectId => *(uint*)(this.Address + ActorOffsets.DataId);
    }
}

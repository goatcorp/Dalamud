using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    ///     This class represents a NPC.
    /// </summary>
    public unsafe class Npc : Chara
    {
        /// <summary>
        /// Set up a new NPC with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        public Npc(IntPtr address, Dalamud dalamud) : base( address, dalamud )
        {
        }

        /// <summary>
        /// The data ID of the NPC linking to their assoicated BNpcBase data.
        /// </summary>
        public uint BaseId => *(uint*)(Address + Structs.ActorOffsets.DataId);

        /// <summary>
        /// The name ID of the NPC linking to their respective game data.
        /// </summary>
        public uint NameId => *(uint*)(Address + Structs.ActorOffsets.NameId);
    }
}

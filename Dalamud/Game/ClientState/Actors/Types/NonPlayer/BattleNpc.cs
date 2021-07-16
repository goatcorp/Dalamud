using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    /// This class represents a battle NPC.
    /// </summary>
    public unsafe class BattleNpc : Npc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BattleNpc"/> class.
        /// Set up a new BattleNpc with the provided memory representation.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        internal BattleNpc(IntPtr address, Dalamud dalamud)
            : base(address, dalamud)
        {
        }

        /// <summary>
        /// Gets the BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
        /// </summary>
        public BattleNpcSubKind BattleNpcKind => *(BattleNpcSubKind*)(this.Address + ActorOffsets.SubKind);

        /// <summary>
        /// Gets the target of the Battle NPC.
        /// </summary>
        public override uint TargetActorID => *(uint*)(this.Address + ActorOffsets.BattleNpcTargetActorId);
    }
}

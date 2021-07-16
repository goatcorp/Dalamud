using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer
{
    /// <summary>
    /// This class represents a battle NPC.
    /// </summary>
    public class BattleNpc : Npc
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BattleNpc"/> class.
        /// Set up a new BattleNpc with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        internal BattleNpc(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud)
            : base(address, actorStruct, dalamud)
        {
        }

        /// <summary>
        /// Gets the BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
        /// </summary>
        public BattleNpcSubKind BattleNpcKind => (BattleNpcSubKind)this.ActorStruct.SubKind;

        /// <summary>
        /// Gets the ID of this BattleNpc's owner.
        /// </summary>
        public int OwnerId => this.ActorStruct.OwnerId;

        /// <summary>
        /// Gets target of the Battle NPC.
        /// </summary>
        public override int TargetActorID => this.ActorStruct.BattleNpcTargetActorId;
    }
}

using System;

namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer {
    /// <summary>
    ///     This class represents a battle NPC.
    /// </summary>
    public class BattleNpc : Npc {
        /// <summary>
        ///     Set up a new BattleNpc with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        public BattleNpc(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud) : base(address, actorStruct, dalamud) { }

        /// <summary>
        ///     The BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
        /// </summary>
        public BattleNpcSubKind BattleNpcKind => (BattleNpcSubKind) this.actorStruct.SubKind;

        /// <summary>
        ///     The ID of this BattleNpc's owner.
        /// </summary>
        public int OwnerId => this.actorStruct.OwnerId;

        /// <summary>
        /// Target of the Battle NPC
        /// </summary>
        public override int TargetActorID => this.actorStruct.BattleNpcTargetActorId;

    }
}

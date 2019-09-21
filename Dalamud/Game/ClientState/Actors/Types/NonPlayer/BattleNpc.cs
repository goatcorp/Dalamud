namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer {
    /// <summary>
    ///     This class represents a battle NPC.
    /// </summary>
    public class BattleNpc : Npc {
        /// <summary>
        ///     Set up a new BattleNpc with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        public BattleNpc(Structs.Actor actorStruct) : base(actorStruct) { }

        /// <summary>
        ///     The BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
        /// </summary>
        public BattleNpcSubKind BattleNpcKind => (BattleNpcSubKind) this.actorStruct.SubKind;

        /// <summary>
        ///     The ID of this BattleNpc's owner.
        /// </summary>
        public int OwnerId => this.actorStruct.OwnerId;
    }
}

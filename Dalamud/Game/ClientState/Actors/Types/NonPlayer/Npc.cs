namespace Dalamud.Game.ClientState.Actors.Types.NonPlayer {
    /// <summary>
    ///     This class represents a NPC.
    /// </summary>
    public class Npc : Chara {
        /// <summary>
        ///     Set up a new NPC with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        protected Npc(Structs.Actor actorStruct, Dalamud dalamud) : base(actorStruct, dalamud) { }

        /// <summary>
        ///     The data ID of the NPC linking to their respective game data.
        /// </summary>
        public int DataId => this.actorStruct.DataId;

        /// <summary>
        ///     The name ID of the NPC linking to their respective game data.
        /// </summary>
        public int NameId => this.actorStruct.NameId;
    }
}

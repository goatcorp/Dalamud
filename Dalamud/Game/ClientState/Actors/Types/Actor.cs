namespace Dalamud.Game.ClientState.Actors.Types {
    /// <summary>
    ///     This class represents a basic FFXIV actor.
    /// </summary>
    public class Actor {
        /// <summary>
        ///     The memory representation of the base actor.
        /// </summary>
        protected Structs.Actor actorStruct;

        /// <summary>
        ///     Initialize a representation of a basic FFXIV actor.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        public Actor(Structs.Actor actorStruct) {
            this.actorStruct = actorStruct;
        }

        /// <summary>
        ///     Position of this <see cref="Actor" />.
        /// </summary>
        public Position3 Position => this.actorStruct.Position;

        /// <summary>
        ///     Displayname of this <see cref="Actor">Actor</see>.
        /// </summary>
        public string Name => this.actorStruct.Name;

        /// <summary>
        ///     Actor ID of this <see cref="Actor" />.
        /// </summary>
        public int ActorId => this.actorStruct.ActorId;

        /// <summary>
        ///     Entity kind of this <see cref="Actor">actor</see>. See <see cref="ObjectKind">the ObjectKind enum</see> for
        ///     possible values.
        /// </summary>
        public ObjectKind ObjectKind => this.actorStruct.ObjectKind;
    }
}

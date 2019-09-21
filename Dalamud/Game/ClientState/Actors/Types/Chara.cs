using Dalamud.Game.ClientState.Actors.Resolvers;

namespace Dalamud.Game.ClientState.Actors.Types {
    /// <summary>
    ///     This class represents the base for non-static entities.
    /// </summary>
    public class Chara : Actor {
        /// <summary>
        ///     Set up a new Chara with the provided memory representation.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        public Chara(Structs.Actor actorStruct) : base(actorStruct) { }

        /// <summary>
        ///     The level of this Chara.
        /// </summary>
        public byte Level => this.actorStruct.Level;

        /// <summary>
        ///     The ClassJob of this Chara.
        /// </summary>
        public ClassJob ClassJob => new ClassJob(this.actorStruct.ClassJob);

        /// <summary>
        ///     The current HP of this Chara.
        /// </summary>
        public int CurrentHp => this.actorStruct.CurrentHp;

        /// <summary>
        ///     The maximum HP of this Chara.
        /// </summary>
        public int MaxHp => this.actorStruct.MaxHp;

        /// <summary>
        ///     The current MP of this Chara.
        /// </summary>
        public int CurrentMp => this.actorStruct.CurrentMp;

        /// <summary>
        ///     The maximum MP of this Chara.
        /// </summary>
        public int MaxMp => this.actorStruct.MaxMp;
    }
}

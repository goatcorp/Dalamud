using System;
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
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        protected Chara(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud) : base(address, actorStruct, dalamud) { }

        /// <summary>
        ///     The level of this Chara.
        /// </summary>
        public byte Level => this.actorStruct.Level;

        /// <summary>
        ///     The ClassJob of this Chara.
        /// </summary>
        public ClassJob ClassJob => new ClassJob(this.actorStruct.ClassJob, this.dalamud);

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

        /// <summary>
        /// Byte array describing the visual appearance of this Chara. Indexed by <see cref="CustomizeIndex"/>.
        /// </summary>
        public byte[] Customize => this.actorStruct.Customize;
    }
}

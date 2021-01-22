using System;
using Dalamud.Game.ClientState.Actors.Resolvers;

namespace Dalamud.Game.ClientState.Actors.Types {
    /// <summary>
    ///     This class represents the base for non-static entities.
    /// </summary>
    public unsafe class Chara : Actor {
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
        public byte Level => *(byte*)(Address + Structs.ActorOffsets.Level);

        /// <summary>
        ///     The ClassJob of this Chara.
        /// </summary>
        public ClassJob ClassJob => new ClassJob(*(byte*)(Address + Structs.ActorOffsets.ClassJob), this.dalamud);

        /// <summary>
        ///     The current HP of this Chara.
        /// </summary>
        public int CurrentHp => *(int*)(Address + Structs.ActorOffsets.CurrentHp);

        /// <summary>
        ///     The maximum HP of this Chara.
        /// </summary>
        public int MaxHp => *(int*)(Address + Structs.ActorOffsets.MaxHp);

        /// <summary>
        ///     The current MP of this Chara.
        /// </summary>
        public int CurrentMp => *(int*)(Address + Structs.ActorOffsets.CurrentMp);

        /// <summary>
        ///     The maximum MP of this Chara.
        /// </summary>
        public int MaxMp => *(int*)(Address + Structs.ActorOffsets.MaxMp);

        /// <summary>
        ///     The current GP of this Chara.
        /// </summary>
        public int CurrentGp => *(int*)(Address + Structs.ActorOffsets.CurrentGp);

        /// <summary>
        ///     The maximum GP of this Chara.
        /// </summary>
        public int MaxGp => *(int*)(Address + Structs.ActorOffsets.MaxGp);

        /// <summary>
        ///     The current CP of this Chara.
        /// </summary>
        public int CurrentCp => *(int*)(Address + Structs.ActorOffsets.CurrentCp);

        /// <summary>
        ///     The maximum CP of this Chara.
        /// </summary>
        public int MaxCp => *(int*)(Address + Structs.ActorOffsets.MaxCp);

        /// <summary>
        /// Byte array describing the visual appearance of this Chara. Indexed by <see cref="CustomizeIndex"/>.
        /// </summary>
        public byte[] Customize => this.actorStruct.Customize;
    }
}

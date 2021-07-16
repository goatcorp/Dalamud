using System;

using Dalamud.Game.ClientState.Actors.Resolvers;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// This class represents the base for non-static entities.
    /// </summary>
    public class Chara : Actor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Chara"/> class.
        /// This represents a non-static entity.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        internal Chara(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud)
            : base(address, actorStruct, dalamud)
        {
        }

        /// <summary>
        /// Gets the level of this Chara.
        /// </summary>
        public byte Level => this.ActorStruct.Level;

        /// <summary>
        /// Gets the ClassJob of this Chara.
        /// </summary>
        public ClassJob ClassJob => new(this.ActorStruct.ClassJob, this.Dalamud);

        /// <summary>
        /// Gets the current HP of this Chara.
        /// </summary>
        public int CurrentHp => this.ActorStruct.CurrentHp;

        /// <summary>
        /// Gets the maximum HP of this Chara.
        /// </summary>
        public int MaxHp => this.ActorStruct.MaxHp;

        /// <summary>
        /// Gets the current MP of this Chara.
        /// </summary>
        public int CurrentMp => this.ActorStruct.CurrentMp;

        /// <summary>
        /// Gets the maximum MP of this Chara.
        /// </summary>
        public int MaxMp => this.ActorStruct.MaxMp;

        /// <summary>
        /// Gets the current GP of this Chara.
        /// </summary>
        public int CurrentGp => this.ActorStruct.CurrentGp;

        /// <summary>
        /// Gets the maximum GP of this Chara.
        /// </summary>
        public int MaxGp => this.ActorStruct.MaxGp;

        /// <summary>
        /// Gets the current CP of this Chara.
        /// </summary>
        public int CurrentCp => this.ActorStruct.CurrentCp;

        /// <summary>
        /// Gets the maximum CP of this Chara.
        /// </summary>
        public int MaxCp => this.ActorStruct.MaxCp;

        /// <summary>
        /// Gets a byte array describing the visual appearance of this Chara.
        /// Indexed by <see cref="CustomizeIndex"/>.
        /// </summary>
        public byte[] Customize => this.ActorStruct.Customize;

        /// <summary>
        /// Gets status Effects.
        /// </summary>
        public StatusFlags StatusFlags => this.ActorStruct.StatusFlags;
    }
}

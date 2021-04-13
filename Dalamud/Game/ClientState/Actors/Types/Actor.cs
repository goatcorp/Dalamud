using System;

using Dalamud.Game.ClientState.Structs;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    ///     This class represents a basic FFXIV actor.
    /// </summary>
    public class Actor : IEquatable<Actor>
    {
        private readonly Structs.Actor actorStruct;
        // This is a breaking change. StyleCop demands it.
        // private readonly IntPtr address;
        private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// This represents a basic FFXIV actor.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        public Actor(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud)
        {
            this.actorStruct = actorStruct;
            this.dalamud = dalamud;
            this.Address = address;
        }

        /// <summary>
        /// Gets position of this <see cref="Actor" />.
        /// </summary>
        public Position3 Position => this.ActorStruct.Position;

        /// <summary>
        /// Gets rotation of this <see cref="Actor" />.
        /// This ranges from -pi to pi radians.
        /// </summary>
        public float Rotation => this.ActorStruct.Rotation;

        /// <summary>
        /// Gets displayname of this <see cref="Actor" />.
        /// </summary>
        public string Name => this.ActorStruct.Name;

        /// <summary>
        /// Gets actor ID of this <see cref="Actor" />.
        /// </summary>
        public int ActorId => this.ActorStruct.ActorId;

        /// <summary>
        /// Gets entity kind of this <see cref="Actor" />.
        /// See <see cref="ObjectKind">the ObjectKind enum</see> for possible values.
        /// </summary>
        public ObjectKind ObjectKind => this.ActorStruct.ObjectKind;

        /// <summary>
        /// Gets the X distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceX => this.ActorStruct.YalmDistanceFromPlayerX;

        /// <summary>
        /// Gets the Y distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceY => this.ActorStruct.YalmDistanceFromPlayerY;

        /// <summary>
        /// Gets the target of the actor.
        /// </summary>
        public virtual int TargetActorID => 0;

        /// <summary>
        ///  Gets status Effects.
        /// </summary>
        public StatusEffect[] StatusEffects => this.ActorStruct.UIStatusEffects;

        /// <summary>
        /// Gets the address of this actor in memory.
        /// </summary>
        // TODO: This is a breaking change, StyleCop demands it.
        // public IntPtr Address => this.address;
        public readonly IntPtr Address;

        /// <summary>
        /// Gets the memory representation of the base actor.
        /// </summary>
        internal Structs.Actor ActorStruct => this.actorStruct;

        /// <summary>
        /// Gets the <see cref="Dalamud"/> backing instance.
        /// </summary>
        protected Dalamud Dalamud => this.dalamud;

        /// <inheritdoc/>
        bool IEquatable<Actor>.Equals(Actor other) => this.ActorId == other.ActorId;
    }
}

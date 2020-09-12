using Dalamud.Game.ClientState.Structs;
using System;

namespace Dalamud.Game.ClientState.Actors.Types {
    /// <summary>
    ///     This class represents a basic FFXIV actor.
    /// </summary>
    public class Actor : IEquatable<Actor> {
        /// <summary>
        ///     The memory representation of the base actor.
        /// </summary>
        protected Structs.Actor actorStruct;

        protected Dalamud dalamud;

        /// <summary>
        /// The address of this actor in memory.
        /// </summary>
        public readonly IntPtr Address;

        /// <summary>
        ///     Initialize a representation of a basic FFXIV actor.
        /// </summary>
        /// <param name="actorStruct">The memory representation of the base actor.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        /// <param name="address">The address of this actor in memory.</param>
        public Actor(IntPtr address, Structs.Actor actorStruct, Dalamud dalamud) {
            this.actorStruct = actorStruct;
            this.dalamud = dalamud;
            this.Address = address;
        }

        /// <summary>
        ///     Position of this <see cref="Actor" />.
        /// </summary>
        public Position3 Position => this.actorStruct.Position;

        /// <summary>
        /// Rotation of this <see cref="Actor"/>.<br/>
        /// This ranges from -pi to pi radians.
        /// </summary>
        public float Rotation => this.actorStruct.Rotation;

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

        /// <summary>
        /// The X distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceX => this.actorStruct.YalmDistanceFromPlayerX;

        /// <summary>
        /// The Y distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceY => this.actorStruct.YalmDistanceFromPlayerY;

        /// <summary>
        /// The target of the actor
        /// </summary>
        public virtual int TargetActorID => 0;

        /// <summary>
        ///  Status Effects
        /// </summary>
        public StatusEffect[] StatusEffects => this.actorStruct.UIStatusEffects;

        bool IEquatable<Actor>.Equals(Actor other) => this.ActorId == other.ActorId;
    }
}

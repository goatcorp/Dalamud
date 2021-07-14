using System;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Structs;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    /// This class represents a basic actor (GameObject) in FFXIV.
    /// </summary>
    public unsafe partial class Actor : IEquatable<Actor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        internal Actor(IntPtr address, Dalamud dalamud)
        {
            this.Dalamud = dalamud;
            this.Address = address;
        }

        /// <summary>
        /// Gets the address of the actor in memory.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets Dalamud itself.
        /// </summary>
        private protected Dalamud Dalamud { get; }

        /// <summary>
        /// This allows you to <c>if (actor) {...}</c> to check for validity.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True or false.</returns>
        public static implicit operator bool(Actor actor) => IsValid(actor);

        /// <summary>
        /// Gets a value indicating whether this actor is still valid in memory.
        /// </summary>
        /// <param name="actor">The actor to check.</param>
        /// <returns>True or false.</returns>
        public static bool IsValid(Actor actor)
        {
            if (actor == null)
                return false;

            if (actor.Dalamud.ClientState.LocalContentId == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether this actor is still valid in memory.
        /// </summary>
        /// <returns>True or false.</returns>
        public bool IsValid() => IsValid(this);

        /// <inheritdoc/>
        bool IEquatable<Actor>.Equals(Actor other) => this.ActorId == other?.ActorId;

        /// <inheritdoc/>
        public override bool Equals(object obj) => ((IEquatable<Actor>)this).Equals(obj as Actor);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
    }

    /// <summary>
    /// This class represents a basic actor (GameObject) in FFXIV.
    /// </summary>
    public unsafe partial class Actor
    {
        /// <summary>
        /// Gets the displayname of this <see cref="Actor" />.
        /// </summary>
        public SeString Name => MemoryHelper.ReadSeString(this.Address + ActorOffsets.Name, 32);

        /// <summary>
        /// Gets the actor ID of this <see cref="Actor" />.
        /// </summary>
        public uint ActorId => *(uint*)(this.Address + ActorOffsets.ActorId);

        /// <summary>
        /// Gets the data ID for linking to other respective game data.
        /// </summary>
        public uint DataId => *(uint*)(this.Address + ActorOffsets.DataId);

        /// <summary>
        /// Gets the ID of this GameObject's owner.
        /// </summary>
        public uint OwnerId => *(uint*)(this.Address + ActorOffsets.OwnerId);

        /// <summary>
        /// Gets the entity kind of this <see cref="Actor" />.
        /// See <see cref="ObjectKind">the ObjectKind enum</see> for possible values.
        /// </summary>
        public ObjectKind ObjectKind => *(ObjectKind*)(this.Address + ActorOffsets.ObjectKind);

        /// <summary>
        /// Gets the sub kind of this Actor.
        /// </summary>
        public byte SubKind => *(byte*)(this.Address + ActorOffsets.SubKind);

        /// <summary>
        /// Gets a value indicating whether the actor is friendly.
        /// </summary>
        public bool IsFriendly => *(int*)(this.Address + ActorOffsets.IsFriendly) > 0;

        /// <summary>
        /// Gets the X distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceX => *(byte*)(this.Address + ActorOffsets.YalmDistanceFromObjectX);

        /// <summary>
        /// Gets the target status.
        /// </summary>
        /// <remarks>
        /// This is some kind of enum. It may be <see cref="StatusEffect"/>.
        /// </remarks>
        public byte TargetStatus => *(byte*)(this.Address + ActorOffsets.TargetStatus);

        /// <summary>
        /// Gets the Y distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceY => *(byte*)(this.Address + ActorOffsets.YalmDistanceFromObjectY);

        /// <summary>
        /// Gets the position of this <see cref="Actor" />.
        /// </summary>
        public Position3 Position => *(Position3*)(this.Address + ActorOffsets.Position);

        /// <summary>
        /// Gets the rotation of this <see cref="Actor" />.
        /// This ranges from -pi to pi radians.
        /// </summary>
        public float Rotation => *(float*)(this.Address + ActorOffsets.Rotation);

        /// <summary>
        /// Gets the hitbox radius of this <see cref="Actor" />.
        /// </summary>
        public float HitboxRadius => *(float*)(this.Address + ActorOffsets.HitboxRadius);

        /// <summary>
        /// Gets the current target of the Actor.
        /// </summary>
        public virtual uint TargetActorID => 0;
    }
}

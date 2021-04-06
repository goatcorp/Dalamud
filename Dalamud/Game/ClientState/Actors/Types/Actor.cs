using Dalamud.Game.ClientState.Structs;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Actors.Types
{
    /// <summary>
    ///     This class represents a basic FFXIV actor.
    /// </summary>
    public unsafe class Actor : IEquatable<Actor>
    {
        protected Dalamud dalamud;

        /// <summary>
        /// The address of this actor in memory.
        /// </summary>
        public readonly IntPtr Address;

        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// </summary>
        /// <param name="address">The address of this actor in memory.</param>
        /// <param name="dalamud">A dalamud reference needed to access game data in Resolvers.</param>
        public Actor(IntPtr address, Dalamud dalamud)
        {
            this.dalamud = dalamud;
            this.Address = address;
        }

        /// <summary>
        ///     Position of this <see cref="Actor" />.
        /// </summary>
        public Position3 Position => *(Position3*)(Address + ActorOffsets.Position);

        /// <summary>
        /// Rotation of this <see cref="Actor"/>.<br/>
        /// This ranges from -pi to pi radians.
        /// </summary>
        public float Rotation => *(float*)(Address + ActorOffsets.Rotation);

        /// <summary>
        ///     Displayname of this <see cref="Actor">Actor</see>.
        /// </summary>
        public string Name => Marshal.PtrToStringAnsi(Address + ActorOffsets.Name);

        /// <summary>
        ///     Actor ID of this <see cref="Actor" />.
        /// </summary>
        public uint ActorId => *(uint*)(Address + ActorOffsets.ActorId);

        /// <summary>
        ///     Entity kind of this <see cref="Actor">actor</see>. See <see cref="ObjectKind">the ObjectKind enum</see> for
        ///     possible values.
        /// </summary>
        public ObjectKind ObjectKind => *(ObjectKind*)(Address + ActorOffsets.ObjectKind);

        /// <summary>
        /// The X distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceX => *(byte*)(Address + ActorOffsets.YalmDistanceFromPlayerX);

        /// <summary>
        /// The Y distance from the local player in yalms.
        /// </summary>
        public byte YalmDistanceY => *(byte*)(Address + ActorOffsets.YalmDistanceFromPlayerY);

        /// <summary>
        /// The target of the actor
        /// </summary>
        public virtual uint TargetActorID => 0;

        /// <summary>
        /// Status Effects.
        /// </summary>
        /// <remarks>
        /// This copies every time it is invoked, so make sure to only grab it once
        /// </remarks>
        public StatusEffect[] StatusEffects
        {
            get
            {
                const int length = 20;
                var effects = new StatusEffect[length];

                var addr = Address + ActorOffsets.UIStatusEffects;
                var size = Marshal.SizeOf< StatusEffect >();
                for (var i = 0; i < length; i++)
                {
                    effects[i] = Marshal.PtrToStructure<StatusEffect>(addr + (i * size));
                }

                return effects;
            }
        }

        /// <inheritdoc/>
        bool IEquatable<Actor>.Equals(Actor other) => this.ActorId == other?.ActorId;

        /// <summary>
        /// Allows you to <code>if (actor) {...}</code> to check for validity.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static implicit operator bool(Actor a) => IsValid(a);

        public static bool IsValid(Actor actor)
        {
            if (actor == null)
            {
                return false;
            }

            // todo: check game state
            if (actor.dalamud.ClientState.LocalContentId == 0)
            {
                return false;
            }

            return true;
        }
    }
}

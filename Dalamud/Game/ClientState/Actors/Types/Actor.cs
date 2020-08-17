using Dalamud.Game.ClientState.Structs;
using System;
using System.Threading.Tasks;

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

        public ObjectVisibility ObjectVisibility => this.actorStruct.ObjectVisibility;

        bool IEquatable<Actor>.Equals(Actor other) => this.ActorId == other.ActorId;

        /// <summary>
        /// Hides the actor
        /// </summary>
        public async void Hide()
        {
            var player = ObjectKind == ObjectKind.Player;

            unsafe
            {
                var kind = (ObjectKind*)(Address + ActorOffsets.ObjectKind);
                var vis = (ObjectVisibility*)(Address + ActorOffsets.ObjectVisibility);
                if (player)
                {
                    *kind = ObjectKind.BattleNpc;
                }

                *vis = ObjectVisibility.Invisible;
            }

            if (player)
            {
                await Task.Delay(100);
                unsafe
                {
                    var kind = (ObjectKind*)(Address + ActorOffsets.ObjectKind);
                    *kind = ObjectKind.Player;
                }
            }
        }

        /// <summary>
        /// Re-renders the actor
        /// </summary>
        public async void ReRender()
        {
            var player = ObjectKind == ObjectKind.Player;

            unsafe
            {
                var kind = (ObjectKind*)(Address + ActorOffsets.ObjectKind);
                var vis = (ObjectVisibility*)(Address + ActorOffsets.ObjectVisibility);
                if (player)
                {
                    *kind = ObjectKind.BattleNpc;
                }
                    
                *vis = ObjectVisibility.Invisible;
            }

            await Task.Delay(100);
            unsafe
            {
                var vis = (ObjectVisibility*)(Address + ActorOffsets.ObjectVisibility);
                *vis = ObjectVisibility.Visible;
            }

            if (player)
            {
                await Task.Delay(100);
                unsafe
                {
                    var kind = (ObjectKind*)(Address + ActorOffsets.ObjectKind);
                    *kind = ObjectKind.Player;
                }
            }
        }
    }
}

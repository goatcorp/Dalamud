using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.ClientState.Structs;
using JetBrains.Annotations;
using Serilog;

using Actor = Dalamud.Game.ClientState.Actors.Types.Actor;

namespace Dalamud.Game.ClientState.Actors
{
    /// <summary>
    ///     This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public class ActorTable : IReadOnlyCollection<Actor>, ICollection
    {
        private const int ActorTableLength = 424;

        private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTable"/> class.
        /// </summary>
        /// <param name="dalamud">The <see cref="dalamud"/> instance.</param>
        /// <param name="addressResolver">Client state address resolver.</param>
        internal ActorTable(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.Address = addressResolver;
            this.dalamud = dalamud;

            Log.Verbose("Actor table address {ActorTable}", this.Address.ActorTable);
        }

        /// <summary>
        /// Gets the amount of currently spawned actors.
        /// </summary>
        public int Length
        {
            get
            {
                var count = 0;
                for (var i = 0; i < ActorTableLength; i++)
                {
                    var ptr = this.GetActorAddress(i);
                    if (ptr != IntPtr.Zero)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <inheritdoc/>
        int IReadOnlyCollection<Actor>.Count => this.Length;

        /// <inheritdoc/>
        int ICollection.Count => this.Length;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        private ClientStateAddressResolver Address { get; }

        /// <summary>
        ///     Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Actor" /> at the specified spawn index.</returns>
        [CanBeNull]
        public Actor this[int index]
        {
            get
            {
                var ptr = this.GetActorAddress(index);
                if (ptr != IntPtr.Zero)
                {
                    return this.CreateActorReference(ptr);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the address of the actor at the specified index of the actor table.
        /// </summary>
        /// <param name="index">The index of the actor.</param>
        /// <returns>The memory address of the actor.</returns>
        public unsafe IntPtr GetActorAddress(int index)
        {
            if (index >= ActorTableLength)
            {
                return IntPtr.Zero;
            }

            return *(IntPtr*)(this.Address.ActorTable + (8 * index));
        }

        /// <inheritdoc/>
        public IEnumerator<Actor> GetEnumerator()
        {
            for (var i = 0; i < ActorTableLength; i++)
            {
                yield return this[i];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < this.Length; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        /// <summary>
        /// Create a reference to a FFXIV actor.
        /// </summary>
        /// <param name="offset">The offset of the actor in memory.</param>
        /// <returns><see cref="Actor"/> object or inheritor containing requested data.</returns>
        [CanBeNull]
        internal unsafe Actor CreateActorReference(IntPtr offset)
        {
            if (this.dalamud.ClientState.LocalContentId == 0)
            {
                return null;
            }

            var objKind = *(ObjectKind*)(offset + ActorOffsets.ObjectKind);

            // TODO: This is for compatibility with legacy actor classes - superseded once ready
            var actorStruct = Marshal.PtrToStructure<Structs.Actor>(offset);

            return objKind switch
            {
                ObjectKind.Player => new PlayerCharacter(offset, actorStruct, this.dalamud),
                ObjectKind.BattleNpc => new BattleNpc(offset, actorStruct, this.dalamud),
                ObjectKind.EventObj => new EventObj(offset, actorStruct, this.dalamud),
                ObjectKind.Companion => new Npc(offset, actorStruct, this.dalamud),
                _ => new Actor(offset, actorStruct, this.dalamud),
            };
        }
    }
}

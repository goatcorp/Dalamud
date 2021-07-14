using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Actors
{
    /// <summary>
    /// This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public sealed partial class ActorTable
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

        private ClientStateAddressResolver Address { get; }

        /// <summary>
        /// Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns>An <see cref="Actor" /> at the specified spawn index.</returns>
        [CanBeNull]
        public Actor this[int index]
        {
            get
            {
                var address = this.GetActorAddress(index);
                return this[address];
            }
        }

        /// <summary>
        /// Get an actor at the specified address.
        /// </summary>
        /// <param name="address">The actor address.</param>
        /// <returns>An <see cref="Actor" /> at the specified address.</returns>
        public Actor this[IntPtr address]
        {
            get
            {
                if (address == IntPtr.Zero)
                    return null;

                return this.CreateActorReference(address);
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

            return objKind switch
            {
                ObjectKind.Player => new PlayerCharacter(offset, this.dalamud),
                ObjectKind.BattleNpc => new BattleNpc(offset, this.dalamud),
                ObjectKind.EventObj => new EventObj(offset, this.dalamud),
                ObjectKind.Companion => new Npc(offset, this.dalamud),
                _ => new Actor(offset, this.dalamud),
            };
        }
    }

    /// <summary>
    /// This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public sealed partial class ActorTable : IReadOnlyCollection<Actor>, ICollection
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<Actor>.Count => this.Length;

        /// <inheritdoc/>
        int ICollection.Count => this.Length;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        public IEnumerator<Actor> GetEnumerator()
        {
            for (var i = 0; i < ActorTableLength; i++)
            {
                yield return this[i];
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < this.Length; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }
    }
}

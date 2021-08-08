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

        private readonly ClientStateAddressResolver address;
        private readonly ClientState clientState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTable"/> class.
        /// </summary>
        /// <param name="addressResolver">Client state address resolver.</param>
        internal ActorTable(ClientStateAddressResolver addressResolver)
        {
            this.clientState = Service<ClientState>.Get();
            this.address = addressResolver;

            Log.Verbose($"Actor table address 0x{this.address.ActorTable.ToInt64():X}");
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

        /// <summary>
        /// Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns>An <see cref="Actor"/> at the specified spawn index.</returns>
        [CanBeNull]
        public Actor this[int index]
        {
            get
            {
                var address = this.GetActorAddress(index);
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
                return IntPtr.Zero;

            return *(IntPtr*)(this.address.ActorTable + (8 * index));
        }

        /// <summary>
        /// Create a reference to a FFXIV actor.
        /// </summary>
        /// <param name="address">The address of the actor in memory.</param>
        /// <returns><see cref="Actor"/> object or inheritor containing requested data.</returns>
        [CanBeNull]
        public unsafe Actor CreateActorReference(IntPtr address)
        {
            if (this.clientState.LocalContentId == 0)
                return null;

            if (address == IntPtr.Zero)
                return null;

            var objKind = *(ObjectKind*)(address + ActorOffsets.ObjectKind);
            return objKind switch
            {
                ObjectKind.Player => new PlayerCharacter(address),
                ObjectKind.BattleNpc => new BattleNpc(address),
                ObjectKind.EventObj => new EventObj(address),
                ObjectKind.Companion => new Npc(address),
                _ => new Actor(address),
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

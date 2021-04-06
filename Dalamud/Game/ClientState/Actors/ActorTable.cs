using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Game.ClientState.Structs;
using JetBrains.Annotations;
using Serilog;
using Actor = Dalamud.Game.ClientState.Actors.Types.Actor;

namespace Dalamud.Game.ClientState.Actors {
    /// <summary>
    ///     This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public class ActorTable : IReadOnlyCollection<Actor>, ICollection, IDisposable {

        private const int ActorTableLength = 424;

        private ClientStateAddressResolver Address { get; }
        private Dalamud dalamud;

        /// <summary>
        ///     Set up the actor table collection.
        /// </summary>
        /// <param name="addressResolver">Client state address resolver.</param>
        public ActorTable(Dalamud dalamud, ClientStateAddressResolver addressResolver) {
            Address = addressResolver;
            this.dalamud = dalamud;

            Log.Verbose("Actor table address {ActorTable}", Address.ActorTable);
        }

        /// <summary>
        ///     Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Actor" /> at the specified spawn index.</returns>
        [CanBeNull]
        public Actor this[int index] {
            get
            {
                var ptr = GetActorAddress(index);
                if (ptr != IntPtr.Zero)
                {
                    return CreateActorReference(ptr);
                }

                return null;
            }
        }

        public unsafe IntPtr GetActorAddress(int index)
        {
            if (index >= ActorTableLength)
            {
                return IntPtr.Zero;
            }

            return *(IntPtr*)(Address.ActorTable + (8 * index));
        }

        internal unsafe Actor CreateActorReference(IntPtr offset)
        {
            if (this.dalamud.ClientState.LocalContentId == 0)
            {
                return null;
            }

            var objKind = *(ObjectKind*)(offset + ActorOffsets.ObjectKind);

            return objKind switch {
                ObjectKind.Player => new PlayerCharacter(offset, this.dalamud),
                ObjectKind.BattleNpc => new BattleNpc(offset, this.dalamud),
                ObjectKind.EventObj => new EventObj(offset, this.dalamud),
                ObjectKind.Companion => new Npc(offset, this.dalamud),
                _ => new Actor(offset, this.dalamud)
            };
        }

        public IEnumerator<Actor> GetEnumerator()
        {
            for (var i = 0; i < ActorTableLength; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// The amount of currently spawned actors.
        /// </summary>
        public int Length
        {
            get
            {
                var count = 0;
                for (var i = 0; i < ActorTableLength; i++)
                {
                    var ptr = GetActorAddress(i);
                    if (ptr != IntPtr.Zero)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        int IReadOnlyCollection<Actor>.Count => Length;

        int ICollection.Count => Length;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < Length; i++) {
                array.SetValue(this[i], index);
                index++;
            }
        }

        #region IDisposable Pattern
        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (this.disposed) return;
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ActorTable()
        {
            Dispose(false);
        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Serilog;

namespace Dalamud.Game.ClientState.Actors {
    /// <summary>
    ///     This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public class ActorTable : ICollection {
        private ClientStateAddressResolver Address { get; }

        /// <summary>
        ///     Set up the actor table collection.
        /// </summary>
        /// <param name="addressResolver">Client state address resolver.</param>
        public ActorTable(ClientStateAddressResolver addressResolver) {
            Address = addressResolver;

            Log.Verbose("Actor table address {ActorTable}", Address.ActorTable);
        }

        /// <summary>
        ///     Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Actor" /> at the specified spawn index.</returns>
        public Actor this[int index] {
            get {
                if (index > Length)
                    return null;

                //Log.Information("Trying to get actor at {0}", index);
                var tblIndex = Address.ActorTable + 8 + index * 8;

                var offset = Marshal.ReadIntPtr(tblIndex);

                //Log.Information("Actor at {0}", offset.ToString());

                if (offset == IntPtr.Zero)
                    return null;

                try {
                    var actorStruct = Marshal.PtrToStructure<Structs.Actor>(offset);

                    //Log.Debug("ActorTable[{0}]: {1} - {2} - {3}", index, tblIndex.ToString("X"), offset.ToString("X"),
                    //          actorStruct.ObjectKind.ToString());

                    switch (actorStruct.ObjectKind)
                    {
                        case ObjectKind.Player: return new PlayerCharacter(actorStruct);
                        case ObjectKind.BattleNpc: return new BattleNpc(actorStruct);
                        default: return new Actor(actorStruct);
                    }
                } catch (AccessViolationException) {
                    return null;
                }
            }
        }

        private class ActorTableEnumerator : IEnumerator {
            private readonly ActorTable table;

            private int currentIndex;

            public ActorTableEnumerator(ActorTable table) {
                this.table = table;
            }

            public bool MoveNext() {
                this.currentIndex++;
                return this.currentIndex != this.table.Length;
            }

            public void Reset() {
                this.currentIndex = 0;
            }

            public object Current => this.table[this.currentIndex];
        }

        public IEnumerator GetEnumerator() {
            return new ActorTableEnumerator(this);
        }

        /// <summary>
        ///     The amount of currently spawned actors.
        /// </summary>
        public int Length => Marshal.ReadInt32(Address.ActorTable);

        int ICollection.Count => Length;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index) {
            for (var i = 0; i < Length; i++) {
                array.SetValue(this[i], index);
                index++;
            }
        }
    }
}

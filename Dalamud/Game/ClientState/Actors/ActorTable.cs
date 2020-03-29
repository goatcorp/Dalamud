using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.ClientState.Actors {
    /// <summary>
    ///     This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public class ActorTable : ICollection, IDisposable {
        private ClientStateAddressResolver Address { get; }
        private Dalamud dalamud;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr SomeActorTableAccessDelegate(IntPtr manager, IntPtr offset);

        private Hook<SomeActorTableAccessDelegate> someActorTableAccessHook;

        private bool isReady = false;
        private IntPtr realActorTablePtr;

        /// <summary>
        ///     Set up the actor table collection.
        /// </summary>
        /// <param name="addressResolver">Client state address resolver.</param>
        public ActorTable(Dalamud dalamud, ClientStateAddressResolver addressResolver) {
            Address = addressResolver;
            this.dalamud = dalamud;

            this.someActorTableAccessHook = new Hook<SomeActorTableAccessDelegate>(Address.SomeActorTableAccess, new SomeActorTableAccessDelegate(SomeActorTableAccessDetour), this);

            Log.Verbose("Actor table address {ActorTable}", Address.ViewportActorTable);
        }

        public void Enable() {
            this.someActorTableAccessHook.Enable();
        }

        public void Dispose() {
            if (!this.isReady)
                this.someActorTableAccessHook.Dispose();
        }

        private IntPtr SomeActorTableAccessDetour(IntPtr manager, IntPtr offset) {
            this.realActorTablePtr = offset;
            this.isReady = true;
            return this.someActorTableAccessHook.Original(manager, offset);
        }

        /// <summary>
        ///     Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Actor" /> at the specified spawn index.</returns>
        public Actor this[int index] {
            get {
                if (!this.isReady)
                    return null;

                if (this.someActorTableAccessHook != null) {
                    this.someActorTableAccessHook.Dispose();
                    this.someActorTableAccessHook = null;
                }

                if (index > Length)
                    return null;

                //Log.Information("Trying to get actor at {0}", index);
                var tblIndex = this.realActorTablePtr + 8 + index * 8;

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
                        case ObjectKind.Player: return new PlayerCharacter(actorStruct, this.dalamud);
                        case ObjectKind.BattleNpc: return new BattleNpc(actorStruct, this.dalamud);
                        default: return new Actor(actorStruct, this.dalamud);
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
        public int Length => !this.isReady ? 0 : Marshal.ReadInt32(this.realActorTablePtr);

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

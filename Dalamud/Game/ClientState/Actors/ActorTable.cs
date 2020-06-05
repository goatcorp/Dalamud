using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using Dalamud.Hooking;
using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Actors {
    /// <summary>
    ///     This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public class ActorTable : IReadOnlyCollection<Actor>, ICollection {

        private const int ActorTableLength = 424;

        #region temporary imports for crash workaround

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        #endregion

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
            get {
                if (index >= Length)
                    return null;

                var tblIndex = Address.ActorTable + index * 8;

                var offset = Marshal.ReadIntPtr(tblIndex);

                //Log.Debug($"Reading actor {index} at {tblIndex.ToInt64():X} pointing to {offset.ToInt64():X}");

                if (offset == IntPtr.Zero)
                    return null;

                // FIXME: hack workaround for trying to access the player on logout, after the main object has been deleted
                var sz = Marshal.SizeOf(typeof(Structs.Actor));
                var actorMem = Marshal.AllocHGlobal(sz); // we arguably could just reuse this
                if (!ReadProcessMemory(Process.GetCurrentProcess().Handle, offset, actorMem, sz, out _)) {
                    Log.Debug("ActorTable - ReadProcessMemory failed: likely player deletion during logout");
                    return null;
                }

                var actorStruct = Marshal.PtrToStructure<Structs.Actor>(actorMem);
                Marshal.FreeHGlobal(actorMem);

                //Log.Debug("ActorTable[{0}]: {1} - {2} - {3}", index, tblIndex.ToString("X"), offset.ToString("X"),
                //          actorStruct.ObjectKind.ToString());
                
                switch (actorStruct.ObjectKind) {
                    case ObjectKind.Player: return new PlayerCharacter(offset, actorStruct, this.dalamud);
                    case ObjectKind.BattleNpc: return new BattleNpc(offset, actorStruct, this.dalamud);
                    default: return new Actor(offset, actorStruct, this.dalamud);
                }
            }
        }

        public IEnumerator<Actor> GetEnumerator() {
            for (int i=0;i<Length;i++){
                if (this[i] != null) {
                    yield return this[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        ///     The amount of currently spawned actors.
        /// </summary>
        public int Length => ActorTableLength;

        int IReadOnlyCollection<Actor>.Count => Length;

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

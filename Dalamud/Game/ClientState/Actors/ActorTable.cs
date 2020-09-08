using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Actors {
    /// <summary>
    ///     This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public class ActorTable : IReadOnlyCollection<Actor>, ICollection, IDisposable {

        private const int ActorTableLength = 424;

        #region Actor Table Cache
        private List<Actor> actorsCache;

        private List<Actor> ActorsCache {
            get {
                if (this.actorsCache != null) return this.actorsCache;
                this.actorsCache = GetActorTable();
                return this.actorsCache;
            }
        }

        private void ResetCache() => actorsCache = null;
        #endregion

        #region ReadProcessMemory Hack

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        private static readonly int ActorMemSize = Marshal.SizeOf(typeof(Structs.Actor));
        private IntPtr actorMem = Marshal.AllocHGlobal(ActorMemSize);
        private IntPtr currentProcessHandle = new IntPtr(-1);

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

            dalamud.Framework.OnUpdateEvent += Framework_OnUpdateEvent;

            Log.Verbose("Actor table address {ActorTable}", Address.ActorTable);
        }

        private void Framework_OnUpdateEvent(Internal.Framework framework) {
            this.ResetCache();
        }

        /// <summary>
        ///     Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Actor" /> at the specified spawn index.</returns>
        [CanBeNull]
        public Actor this[int index] {
            get => ActorsCache[index];
        }

        internal Actor ReadActorFromMemory(IntPtr offset)
        {
            try {
                // FIXME: hack workaround for trying to access the player on logout, after the main object has been deleted
                if (!ReadProcessMemory(this.currentProcessHandle, offset, this.actorMem, ActorMemSize, out _))
                {
                    Log.Debug("ActorTable - ReadProcessMemory failed: likely player deletion during logout");
                    return null;
                }

                var actorStruct = Marshal.PtrToStructure<Structs.Actor>(this.actorMem);

                return actorStruct.ObjectKind switch {
                    ObjectKind.Player => new PlayerCharacter(offset, actorStruct, this.dalamud),
                    ObjectKind.BattleNpc => new BattleNpc(offset, actorStruct, this.dalamud),
                    ObjectKind.EventObj => new EventObj(offset, actorStruct, this.dalamud),
                    _ => new Actor(offset, actorStruct, this.dalamud)
                };
            }
            catch (Exception e) {
                Log.Information($"{e}");
                return null;
            }
        }

        private IntPtr[] GetPointerTable() {
            var ret = new IntPtr[ActorTableLength];
            Marshal.Copy(Address.ActorTable, ret, 0, ActorTableLength);
            return ret;
        }

        private List<Actor> GetActorTable() {
            var actors = new List<Actor>();
            var ptrTable = GetPointerTable();
            for (var i = 0; i < ActorTableLength; i++) {
                actors.Add(ptrTable[i] != IntPtr.Zero ? ReadActorFromMemory(ptrTable[i]) : null);
            }
            return actors;
        }

        public IEnumerator<Actor> GetEnumerator() {
            return ActorsCache.Where(a => a != null).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        ///     The amount of currently spawned actors.
        /// </summary>
        public int Length => ActorsCache.Count;

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

        #region IDisposable Pattern
        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (this.disposed) return;
            this.dalamud.Framework.OnUpdateEvent -= Framework_OnUpdateEvent;
            Marshal.FreeHGlobal(this.actorMem);
            this.disposed = true;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ActorTable() {
            Dispose(false);
        }
        #endregion
    }
}

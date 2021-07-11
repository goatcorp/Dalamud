using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Types.NonPlayer;
using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Actors
{
    /// <summary>
    /// This collection represents the currently spawned FFXIV actors.
    /// </summary>
    public sealed partial class ActorTable : IReadOnlyCollection<Actor>, ICollection, IDisposable
    {
        private const int ActorTableLength = 424;

        #region ReadProcessMemory Hack
        private static readonly int ActorMemSize = Marshal.SizeOf(typeof(Structs.Actor));
        private static readonly IntPtr ActorMem = Marshal.AllocHGlobal(ActorMemSize);
        private static readonly IntPtr CurrentProcessHandle = new(-1);
        #endregion

        private Dalamud dalamud;
        private ClientStateAddressResolver address;
        private List<Actor> actorsCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorTable"/> class.
        /// Set up the actor table collection.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        /// <param name="addressResolver">The ClientStateAddressResolver instance.</param>
        internal ActorTable(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.address = addressResolver;
            this.dalamud = dalamud;

            dalamud.Framework.OnUpdateEvent += this.Framework_OnUpdateEvent;

            Log.Verbose($"Actor table address 0x{this.address.ActorTable.ToInt64():X}");
        }

        /// <summary>
        /// Gets the amount of currently spawned actors.
        /// </summary>
        public int Length => this.ActorsCache.Count;

        private List<Actor> ActorsCache => this.actorsCache ??= this.GetActorTable();

        /// <summary>
        /// Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Actor" /> at the specified spawn index.</returns>
        [CanBeNull]
        public Actor this[int index] => this.ActorsCache[index];

        /// <summary>
        /// Read an actor struct from memory and create the appropriate <see cref="ObjectKind"/> type of actor.
        /// </summary>
        /// <param name="offset">Offset of the actor in the actor table.</param>
        /// <returns>An instantiated actor.</returns>
        internal Actor ReadActorFromMemory(IntPtr offset)
        {
            try
            {
                // FIXME: hack workaround for trying to access the player on logout, after the main object has been deleted
                if (!NativeFunctions.ReadProcessMemory(CurrentProcessHandle, offset, ActorMem, ActorMemSize, out _))
                {
                    Log.Debug("ActorTable - ReadProcessMemory failed: likely player deletion during logout");
                    return null;
                }

                var actorStruct = Marshal.PtrToStructure<Structs.Actor>(ActorMem);

                return actorStruct.ObjectKind switch
                {
                    ObjectKind.Player => new PlayerCharacter(offset, actorStruct, this.dalamud),
                    ObjectKind.BattleNpc => new BattleNpc(offset, actorStruct, this.dalamud),
                    ObjectKind.EventObj => new EventObj(offset, actorStruct, this.dalamud),
                    ObjectKind.Companion => new Npc(offset, actorStruct, this.dalamud),
                    _ => new Actor(offset, actorStruct, this.dalamud),
                };
            }
            catch (Exception e)
            {
                Log.Error(e, "Could not read actor from memory.");
                return null;
            }
        }

        private void ResetCache() => this.actorsCache = null;

        private void Framework_OnUpdateEvent(Internal.Framework framework)
        {
            this.ResetCache();
        }

        private IntPtr[] GetPointerTable()
        {
            var ret = new IntPtr[ActorTableLength];
            Marshal.Copy(this.address.ActorTable, ret, 0, ActorTableLength);
            return ret;
        }

        private List<Actor> GetActorTable()
        {
            var actors = new List<Actor>();
            var ptrTable = this.GetPointerTable();
            for (var i = 0; i < ActorTableLength; i++)
            {
                actors.Add(ptrTable[i] != IntPtr.Zero ? this.ReadActorFromMemory(ptrTable[i]) : null);
            }

            return actors;
        }
    }

    /// <summary>
    /// Implementing IDisposable.
    /// </summary>
    public sealed partial class ActorTable : IDisposable
    {
        private bool disposed = false;

        /// <summary>
        /// Finalizes an instance of the <see cref="ActorTable"/> class.
        /// </summary>
        ~ActorTable() => this.Dispose(false);

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (this.disposed)
                return;

            if (disposing)
            {
                this.dalamud.Framework.OnUpdateEvent -= this.Framework_OnUpdateEvent;
                Marshal.FreeHGlobal(ActorMem);
            }

            this.disposed = true;
        }
    }

    /// <summary>
    /// Implementing IReadOnlyCollection, IEnumerable, and Enumerable.
    /// </summary>
    public sealed partial class ActorTable : IReadOnlyCollection<Actor>
    {
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        /// <returns>The number of elements in the collection.</returns>
        int IReadOnlyCollection<Actor>.Count => this.Length;

        /// <summary>
        /// Gets an enumerator capable of iterating through the actor table.
        /// </summary>
        /// <returns>An actor enumerable.</returns>
        public IEnumerator<Actor> GetEnumerator() => this.ActorsCache.Where(a => a != null).GetEnumerator();

        /// <summary>
        /// Gets an enumerator capable of iterating through the actor table.
        /// </summary>
        /// <returns>An actor enumerable.</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>
    /// Implementing ICollection.
    /// </summary>
    public sealed partial class ActorTable : ICollection
    {
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        /// <returns>The number of elements in the collection.</returns>
        int ICollection.Count => this.Length;

        /// <summary>
        /// Gets a value indicating whether access to the collection is synchronized (thread safe).
        /// </summary>
        /// <returns>Whether access is synchronized (thread safe) or not.</returns>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the collection.
        /// </summary>
        /// <returns>An object that can be used to synchronize access to the collection.</returns>
        object ICollection.SyncRoot => this;

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.
        /// </param>
        /// <param name="index">
        /// The zero-based index in array at which copying begins.
        /// </param>
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

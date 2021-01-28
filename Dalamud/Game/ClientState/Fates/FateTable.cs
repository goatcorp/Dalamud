using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using JetBrains.Annotations;
using Serilog;
#pragma warning disable 1591

namespace Dalamud.Game.ClientState.Fates
{
    public class FateTable : IReadOnlyCollection<Fate>, ICollection, IDisposable
    {

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void FateTableDelegate(IntPtr singleton);
        private readonly Hook<FateTableDelegate> fateTableHook;

        private IntPtr firstFate;
        private IntPtr lastFate;

        private static readonly int FateMemSize = Marshal.SizeOf(typeof(Structs.Fate));
        private readonly IntPtr fateMem = Marshal.AllocHGlobal(FateMemSize);

        private List<Fate> fatesCache;

        private List<Fate> FatesCache
        {
            get
            {
                if (this.fatesCache != null) return this.fatesCache;
                this.fatesCache = GetFateTable();
                return this.fatesCache;
            }
        }

        private void ResetCache() => fatesCache = null;

        private ClientStateAddressResolver Address { get; }
        private readonly Dalamud dalamud;

        /// <summary>
        /// Set up the fate table collection.
        /// </summary>
        public FateTable(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            Address = addressResolver;
            this.dalamud = dalamud;

            this.dalamud.Framework.OnUpdateEvent += Framework_OnUpdateEvent;

            Log.Verbose("Fate manager address " + Address.FateTable);

            this.fateTableHook = new Hook<FateTableDelegate>(Address.FateTable, new FateTableDelegate(FateTableDetour));
        }

        private void FateTableDetour(IntPtr manager)
        {
            this.firstFate = Marshal.ReadIntPtr(manager + 0x16E0);
            this.lastFate = Marshal.ReadIntPtr(manager + 0x16E8);

            this.fateTableHook.Original(manager);
        }

        public void Enable()
        {
            this.fateTableHook.Enable();
        }

        private void Framework_OnUpdateEvent(Internal.Framework framework)
        {
            this.ResetCache();
        }

        /// <summary>
        /// Get a fate at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns><see cref="Fate" /> at the specified spawn index.</returns>
        [CanBeNull]
        public Fate this[int index]
        {
            get => FatesCache[index];
        }

        internal Fate ReadFateFromMemory(IntPtr ptr)
        {
            if (SafeMemory.ReadBytes(ptr, FateMemSize, this.fateMem))
            {
                var fateStruct = Marshal.PtrToStructure<Structs.Fate>(this.fateMem);
                return new Fate(ptr, fateStruct);
            }
            return null;
        }

        private List<Fate> GetFateTable()
        {
            var fates = new List<Fate>();

            IntPtr current = this.firstFate;
            IntPtr end = this.lastFate;

            while (current != end)
            {
                SafeMemory.Read<IntPtr>(current, out var fatePtr);
                fates.Add(fatePtr != IntPtr.Zero ? ReadFateFromMemory(fatePtr) : null);
                current += 8;
            }

            return fates;
        }

        public IEnumerator<Fate> GetEnumerator()
        {
            return FatesCache.Where(f => f != null).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The amount of currently spawned fates.
        /// </summary>
        public int Length => FatesCache.Count;

        int IReadOnlyCollection<Fate>.Count => Length;

        int ICollection.Count => Length;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            for (var i = 0; i < Length; i++)
            {
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

            if (disposing)
            {
                this.fateTableHook?.Dispose();
            }

            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FateTable()
        {
            Dispose(false);
        }
        #endregion
    }
}

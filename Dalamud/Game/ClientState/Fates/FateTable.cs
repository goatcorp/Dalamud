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
        private const int checkPtrOffset = 0x80; // If the pointer at this offset is 0, do not scan table
        private const int firstPtrOffset = 0x90;
        private const int lastPtrOffset = 0x98;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void FateTableDelegate(IntPtr singleton);
        private readonly Hook<FateTableDelegate> fateTableHook;


        private List<Fate> fatesCache;
        private List<Fate> FatesCache => this.fatesCache ??= this.GetFateTable();
        private void ResetCache() => fatesCache = null;

        private ClientStateAddressResolver Address { get; }
        private ClientState ClientState { get; }
        private readonly Dalamud dalamud;

        private IntPtr manager;

        /// <summary>
        /// Set up the fate table collection.
        /// </summary>
        public FateTable(Dalamud dalamud, ClientState clientState, ClientStateAddressResolver addressResolver)
        {
            Address = addressResolver;
            this.dalamud = dalamud;
            this.ClientState = clientState;

            this.dalamud.Framework.OnUpdateEvent += Framework_OnUpdateEvent;
            this.ClientState.OnLogout += ClientState_OnLogout;

            Log.Verbose("Fate manager hook address: " + Address.FateTable);

            this.fateTableHook = new Hook<FateTableDelegate>(Address.FateTable, new FateTableDelegate(FateTableDetour));
        }

        /// <summary>
        /// This needs to be done or else risk a crash
        /// The data pointed by at this pointer effectively becomes deallocated and/or corrupted
        /// It seem the game's Fate Manaager is completely deallocated / destructed when logging out
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientState_OnLogout(object sender, EventArgs e)
        {
            this.manager = IntPtr.Zero;
        }

        private void FateTableDetour(IntPtr manager)
        {
            if (this.manager != manager)
            {
                Log.Verbose($"Fate manager address changed to 0x{manager.ToString("X8")}");
            }
            this.manager = manager;
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

        private List<IntPtr> GetPointerTable()
        {
            var ret = new List<IntPtr>();
            IntPtr current = Marshal.ReadIntPtr(manager + firstPtrOffset);
            IntPtr end = Marshal.ReadIntPtr(manager + lastPtrOffset);

            while (current != end)
            {
                var fatePtr = Marshal.ReadIntPtr(current);
                ret.Add(fatePtr);
                current += 8;
            }

            return ret;
        }

        private List<Fate> GetFateTable()
        {
            var fates = new List<Fate>();

            if (this.manager == IntPtr.Zero) return fates;
            if (Marshal.ReadIntPtr(this.manager + checkPtrOffset) == IntPtr.Zero) return fates;

            var currentTerritory = this.dalamud.ClientState.TerritoryType;
            if (currentTerritory == 0) return fates;

            var ptrTable = GetPointerTable();
            foreach (var ptr in ptrTable.Distinct())
            {
                var fateStruct = Marshal.PtrToStructure<Structs.Fate>(ptr);
                if (fateStruct.TerritoryId != currentTerritory) break;
                var fate = new Fate(ptr, fateStruct, dalamud);
                fates.Add(fate);
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
            this.ClientState.OnLogout -= ClientState_OnLogout;
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

using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Fates.Types;
using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Fates
{
    /// <summary>
    /// This collection represents the currently available Fate events.
    /// </summary>
    public sealed partial class FateTable
    {
        // If the pointer at this offset is 0, do not scan the table
        private const int CheckPtrOffset = 0x80;
        private const int FirstPtrOffset = 0x90;
        private const int LastPtrOffset = 0x98;

        private readonly Dalamud dalamud;
        private readonly ClientStateAddressResolver address;

        /// <summary>
        /// Initializes a new instance of the <see cref="FateTable"/> class.
        /// </summary>
        /// <param name="dalamud">The <see cref="dalamud"/> instance.</param>
        /// <param name="addressResolver">Client state address resolver.</param>
        internal FateTable(Dalamud dalamud, ClientStateAddressResolver addressResolver)
        {
            this.address = addressResolver;
            this.dalamud = dalamud;

            Log.Verbose($"Fate table address 0x{this.address.FateTablePtr.ToInt64():X}");
        }

        /// <summary>
        /// Gets the amount of currently active Fates.
        /// </summary>
        public unsafe int Length
        {
            get
            {
                var fateTable = this.FateTableAddress;
                if (fateTable == IntPtr.Zero)
                    return 0;

                var check = *(long*)(fateTable + CheckPtrOffset);
                if (check == 0)
                    return 0;

                var start = *(long*)(fateTable + FirstPtrOffset);
                var end = *(long*)(fateTable + LastPtrOffset);
                if (start == 0 || end == 0)
                    return 0;

                return (int)((end - start) / 8);
            }
        }

        private unsafe IntPtr FateTableAddress
        {
            get
            {
                if (this.address.FateTablePtr == IntPtr.Zero)
                    return IntPtr.Zero;

                return *(IntPtr*)this.address.FateTablePtr;
            }
        }

        /// <summary>
        /// Get an actor at the specified spawn index.
        /// </summary>
        /// <param name="index">Spawn index.</param>
        /// <returns>A <see cref="Fate"/> at the specified spawn index.</returns>
        [CanBeNull]
        public Fate this[int index]
        {
            get
            {
                var address = this.GetFateAddress(index);
                return this[address];
            }
        }

        /// <summary>
        /// Get a Fate at the specified address.
        /// </summary>
        /// <param name="address">The Fate address.</param>
        /// <returns>A <see cref="Fate"/> at the specified address.</returns>
        public Fate this[IntPtr address]
        {
            get
            {
                if (address == IntPtr.Zero)
                    return null;

                return this.CreateFateReference(address);
            }
        }

        /// <summary>
        /// Gets the address of the Fate at the specified index of the fate table.
        /// </summary>
        /// <param name="index">The index of the Fate.</param>
        /// <returns>The memory address of the Fate.</returns>
        public unsafe IntPtr GetFateAddress(int index)
        {
            if (index >= this.Length)
                return IntPtr.Zero;

            var fateTable = this.FateTableAddress;
            if (fateTable == IntPtr.Zero)
                return IntPtr.Zero;

            var firstFate = *(IntPtr*)(fateTable + FirstPtrOffset);
            return *(IntPtr*)(firstFate + (8 * index));
        }

        /// <summary>
        /// Create a reference to a FFXIV actor.
        /// </summary>
        /// <param name="offset">The offset of the actor in memory.</param>
        /// <returns><see cref="Fate"/> object containing requested data.</returns>
        [CanBeNull]
        internal unsafe Fate CreateFateReference(IntPtr offset)
        {
            if (this.dalamud.ClientState.LocalContentId == 0)
                return null;

            if (offset == IntPtr.Zero)
                return null;

            return new Fate(offset, this.dalamud);
        }
    }

    /// <summary>
    /// This collection represents the currently available Fate events.
    /// </summary>
    public sealed partial class FateTable : IReadOnlyCollection<Fate>, ICollection
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<Fate>.Count => this.Length;

        /// <inheritdoc/>
        int ICollection.Count => this.Length;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        public IEnumerator<Fate> GetEnumerator()
        {
            for (var i = 0; i < this.Length; i++)
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

using System;
using System.Collections;
using System.Collections.Generic;

using JetBrains.Annotations;
using Serilog;

namespace Dalamud.Game.ClientState.Fates
{
    /// <summary>
    /// This collection represents the currently available Fate events.
    /// </summary>
    public sealed partial class FateTable
    {
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

                // Sonar used this to check if the table was safe to read
                var check = Struct->Unk80.ToInt64();
                if (check == 0)
                    return 0;

                var start = Struct->FirstFatePtr.ToInt64();
                var end = Struct->LastFatePtr.ToInt64();
                if (start == 0 || end == 0)
                    return 0;

                return (int)((end - start) / 8);
            }
        }

        /// <summary>
        /// Gets the address of the Fate table.
        /// </summary>
        internal unsafe IntPtr FateTableAddress
        {
            get
            {
                if (this.address.FateTablePtr == IntPtr.Zero)
                    return IntPtr.Zero;

                return *(IntPtr*)this.address.FateTablePtr;
            }
        }

        private unsafe FFXIVClientStructs.FFXIV.Client.Game.Fate.FateManager* Struct => (FFXIVClientStructs.FFXIV.Client.Game.Fate.FateManager*)this.FateTableAddress;

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

            var firstFate = this.Struct->FirstFatePtr;
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

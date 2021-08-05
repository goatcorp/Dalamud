using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

namespace Dalamud.Game.ClientState.Statuses
{
    /// <summary>
    /// This collection represents the status effects an actor is afflicted by.
    /// </summary>
    public sealed unsafe partial class StatusList
    {
        private const int StatusListLength = 30;

        private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusList"/> class.
        /// </summary>
        /// <param name="address">Address of the status list.</param>
        /// <param name="dalamud">The <see cref="dalamud"/> instance.</param>
        internal StatusList(IntPtr address, Dalamud dalamud)
        {
            this.Address = address;
            this.dalamud = dalamud;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusList"/> class.
        /// </summary>
        /// <param name="pointer">Pointer to the status list.</param>
        /// <param name="dalamud">The <see cref="dalamud"/> instance.</param>
        internal unsafe StatusList(void* pointer, Dalamud dalamud)
            : this((IntPtr)pointer, dalamud)
        {
        }

        /// <summary>
        /// Gets the address of the status list in memory.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets the amount of status effects the actor has.
        /// </summary>
        public int Length
        {
            get
            {
                var i = 0;
                for (; i < StatusListLength; i++)
                {
                    var status = this[i];
                    if (status == null || status.StatusID == 0)
                        break;
                }

                return i;
            }
        }

        private static int StatusSize { get; } = Marshal.SizeOf<FFXIVClientStructs.FFXIV.Client.Game.Status>();

        private FFXIVClientStructs.FFXIV.Client.Game.StatusManager* Struct => (FFXIVClientStructs.FFXIV.Client.Game.StatusManager*)this.Address;

        /// <summary>
        /// Get a status effect at the specified index.
        /// </summary>
        /// <param name="index">Status Index.</param>
        /// <returns>The status at the specified index.</returns>
        [CanBeNull]
        public Status this[int index]
        {
            get
            {
                if (index < 0 || index > StatusListLength)
                    return null;

                var addr = this.GetStatusAddress(index);
                return this.CreateStatusReference(addr);
            }
        }

        /// <summary>
        /// Gets the address of the party member at the specified index of the party list.
        /// </summary>
        /// <param name="index">The index of the party member.</param>
        /// <returns>The memory address of the party member.</returns>
        public IntPtr GetStatusAddress(int index)
        {
            if (index < 0 || index >= StatusListLength)
                return IntPtr.Zero;

            return (IntPtr)(this.Struct->Status + (index * StatusSize));
        }

        /// <summary>
        /// Create a reference to an FFXIV actor status.
        /// </summary>
        /// <param name="address">The address of the status effect in memory.</param>
        /// <returns>The status object containing the requested data.</returns>
        [CanBeNull]
        public Status CreateStatusReference(IntPtr address)
        {
            if (this.dalamud.ClientState.LocalContentId == 0)
                return null;

            if (address == IntPtr.Zero)
                return null;

            return new Status(address, this.dalamud);
        }
    }

    /// <summary>
    /// This collection represents the status effects an actor is afflicted by.
    /// </summary>
    public sealed partial class StatusList : IReadOnlyCollection<Status>, ICollection
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<Status>.Count => this.Length;

        /// <inheritdoc/>
        int ICollection.Count => this.Length;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        public IEnumerator<Status> GetEnumerator()
        {
            for (var i = 0; i < StatusListLength; i++)
            {
                var status = this[i];

                if (status == null || status.StatusID == 0)
                    continue;

                yield return status;
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

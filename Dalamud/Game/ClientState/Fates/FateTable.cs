using System;
using System.Collections;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Fates
{
    /// <summary>
    /// This collection represents the currently available Fate events.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed partial class FateTable
    {
        private readonly ClientStateAddressResolver address;

        /// <summary>
        /// Initializes a new instance of the <see cref="FateTable"/> class.
        /// </summary>
        /// <param name="addressResolver">Client state address resolver.</param>
        internal FateTable(ClientStateAddressResolver addressResolver)
        {
            this.address = addressResolver;

            Log.Verbose($"Fate table address 0x{this.address.FateTablePtr.ToInt64():X}");
        }

        /// <summary>
        /// Gets the address of the Fate table.
        /// </summary>
        public IntPtr Address => this.address.FateTablePtr;

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
                if (Struct->FateDirector == null)
                    return 0;

                if (Struct->Fates.First == null || Struct->Fates.Last == null)
                    return 0;

                return (int)Struct->Fates.Size();
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
        public Fate? this[int index]
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

            return (IntPtr)this.Struct->Fates.Get((ulong)index).Value;
        }

        /// <summary>
        /// Create a reference to a FFXIV actor.
        /// </summary>
        /// <param name="offset">The offset of the actor in memory.</param>
        /// <returns><see cref="Fate"/> object containing requested data.</returns>
        public Fate? CreateFateReference(IntPtr offset)
        {
            var clientState = Service<ClientState>.Get();

            if (clientState.LocalContentId == 0)
                return null;

            if (offset == IntPtr.Zero)
                return null;

            return new Fate(offset);
        }
    }

    /// <summary>
    /// This collection represents the currently available Fate events.
    /// </summary>
    public sealed partial class FateTable : IReadOnlyCollection<Fate>
    {
        /// <inheritdoc/>
        int IReadOnlyCollection<Fate>.Count => this.Length;

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
    }
}

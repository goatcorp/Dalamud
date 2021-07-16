using System;
using System.Runtime.InteropServices;

using Dalamud.Game.Internal.Gui.Structs;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Gui
{
    /// <summary>
    /// This class handles interacting with the native PartyFinder window.
    /// </summary>
    public sealed class PartyFinderGui : IDisposable
    {
        private readonly Dalamud dalamud;
        private readonly PartyFinderAddressResolver address;
        private readonly IntPtr memory;

        private readonly Hook<ReceiveListingDelegate> receiveListingHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyFinderGui"/> class.
        /// </summary>
        /// <param name="scanner">The SigScanner instance.</param>
        /// <param name="dalamud">The Dalamud instance.</param>
        internal PartyFinderGui(SigScanner scanner, Dalamud dalamud)
        {
            this.dalamud = dalamud;

            this.address = new PartyFinderAddressResolver();
            this.address.Setup(scanner);

            this.memory = Marshal.AllocHGlobal(PartyFinder.PacketInfo.PacketSize);

            this.receiveListingHook = new Hook<ReceiveListingDelegate>(this.address.ReceiveListing, new ReceiveListingDelegate(this.HandleReceiveListingDetour));
        }

        /// <summary>
        /// Event type fired each time the game receives an individual Party Finder listing.
        /// Cannot modify listings but can hide them.
        /// </summary>
        /// <param name="listing">The listings received.</param>
        /// <param name="args">Additional arguments passed by the game.</param>
        public delegate void PartyFinderListingEventDelegate(PartyFinderListing listing, PartyFinderListingEventArgs args);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ReceiveListingDelegate(IntPtr managerPtr, IntPtr data);

        /// <summary>
        /// Event fired each time the game receives an individual Party Finder listing.
        /// Cannot modify listings but can hide them.
        /// </summary>
        public event PartyFinderListingEventDelegate ReceiveListing;

        /// <summary>
        /// Enables this module.
        /// </summary>
        public void Enable()
        {
            this.receiveListingHook.Enable();
        }

        /// <summary>
        /// Dispose of m anaged and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.receiveListingHook.Dispose();
            Marshal.FreeHGlobal(this.memory);
        }

        private void HandleReceiveListingDetour(IntPtr managerPtr, IntPtr data)
        {
            try
            {
                this.HandleListingEvents(data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on ReceiveListing hook.");
            }

            this.receiveListingHook.Original(managerPtr, data);
        }

        private void HandleListingEvents(IntPtr data)
        {
            var dataPtr = data + 0x10;

            var packet = Marshal.PtrToStructure<PartyFinder.Packet>(dataPtr);

            // rewriting is an expensive operation, so only do it if necessary
            var needToRewrite = false;

            for (var i = 0; i < packet.Listings.Length; i++)
            {
                // these are empty slots that are not shown to the player
                if (packet.Listings[i].IsNull())
                {
                    continue;
                }

                var listing = new PartyFinderListing(packet.Listings[i], this.dalamud.Data, this.dalamud.SeStringManager);
                var args = new PartyFinderListingEventArgs(packet.BatchNumber);
                this.ReceiveListing?.Invoke(listing, args);

                if (args.Visible)
                {
                    continue;
                }

                // hide the listing from the player by setting it to a null listing
                packet.Listings[i] = default;
                needToRewrite = true;
            }

            if (!needToRewrite)
            {
                return;
            }

            // write our struct into the memory (doing this directly crashes the game)
            Marshal.StructureToPtr(packet, this.memory, false);

            // copy our new memory over the game's
            unsafe
            {
                Buffer.MemoryCopy((void*)this.memory, (void*)dataPtr, PartyFinder.PacketInfo.PacketSize, PartyFinder.PacketInfo.PacketSize);
            }
        }
    }

    /// <summary>
    /// This class represents additional arguments passed by the game.
    /// </summary>
    public class PartyFinderListingEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartyFinderListingEventArgs"/> class.
        /// </summary>
        /// <param name="batchNumber">The batch number.</param>
        internal PartyFinderListingEventArgs(int batchNumber)
        {
            this.BatchNumber = batchNumber;
        }

        /// <summary>
        /// Gets the batch number.
        /// </summary>
        public int BatchNumber { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the listing is visible.
        /// </summary>
        public bool Visible { get; set; } = true;
    }
}

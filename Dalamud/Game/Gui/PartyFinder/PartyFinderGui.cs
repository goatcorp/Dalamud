using System;
using System.Runtime.InteropServices;

using Dalamud.Game.Gui.PartyFinder.Internal;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.Gui.PartyFinder
{
    /// <summary>
    /// This class handles interacting with the native PartyFinder window.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public sealed class PartyFinderGui : IDisposable
    {
        private readonly PartyFinderAddressResolver address;
        private readonly IntPtr memory;

        private readonly Hook<ReceiveListingDelegate> receiveListingHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartyFinderGui"/> class.
        /// </summary>
        internal PartyFinderGui()
        {
            this.address = new PartyFinderAddressResolver();
            this.address.Setup();

            this.memory = Marshal.AllocHGlobal(PartyFinderPacket.PacketSize);

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
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.receiveListingHook.Dispose();

            try
            {
                Marshal.FreeHGlobal(this.memory);
            }
            catch (BadImageFormatException)
            {
                Log.Warning("Could not free PartyFinderGui memory.");
            }
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

            var packet = Marshal.PtrToStructure<PartyFinderPacket>(dataPtr);

            // rewriting is an expensive operation, so only do it if necessary
            var needToRewrite = false;

            for (var i = 0; i < packet.Listings.Length; i++)
            {
                // these are empty slots that are not shown to the player
                if (packet.Listings[i].IsNull())
                {
                    continue;
                }

                var listing = new PartyFinderListing(packet.Listings[i]);
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
                Buffer.MemoryCopy((void*)this.memory, (void*)dataPtr, PartyFinderPacket.PacketSize, PartyFinderPacket.PacketSize);
            }
        }
    }
}

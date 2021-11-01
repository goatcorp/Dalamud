using System;
using System.Runtime.InteropServices;

using Dalamud.Game.Gui.FellowshipFinder.Internal;
using Dalamud.Game.Gui.FellowshipFinder.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.Gui.FellowshipFinder
{
    /// <summary>
    /// This class handles interacting with the native FellowshipFinder window.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public class FellowshipFinderGui : IDisposable
    {
        private readonly FellowshipFinderAddressResolver address;

        private readonly Hook<ReceiveListingDelegate> receiveListingHook;

        /// <summary>
        /// Initializes a new instance of the <see cref="FellowshipFinderGui"/> class.
        /// </summary>
        internal FellowshipFinderGui()
        {
            this.address = new FellowshipFinderAddressResolver();
            this.address.Setup();

            this.receiveListingHook = new Hook<ReceiveListingDelegate>(this.address.ReceiveListing, this.HandleReceiveListing);
        }

        /// <summary>
        /// Event type fired each time the game receives an individual Fellowship Finder listing.
        /// Cannot modify listings but can hide them.
        /// </summary>
        /// <param name="listing">The listings received.</param>
        /// <param name="args">Additional arguments passed by the game.</param>
        public delegate void FellowshipFinderListingEventDelegate(FellowshipFinderListing listing, FellowshipFinderListingEventArgs args);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ReceiveListingDelegate(IntPtr managerPtr, IntPtr data);

        /// <summary>
        /// Event fired each time the game receives an individual Fellowship Finder listing.
        /// Cannot modify listings but can hide them.
        /// </summary>
        public event FellowshipFinderListingEventDelegate? ReceiveListing;

        /// <summary>
        /// Enables this module.
        /// </summary>
        public void Enable()
        {
            this.receiveListingHook.Enable();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.receiveListingHook.Dispose();
        }

        private void HandleReceiveListing(IntPtr managerPtr, IntPtr data)
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
            var packet = Marshal.PtrToStructure<FellowshipFinderPacket>(data);

            var needToRewrite = false;

            for (var i = 0; i < packet.Listings.Length; i++)
            {
                // don't show empty listings
                if (packet.Listings[i].Id == 0)
                {
                    continue;
                }

                var listing = new FellowshipFinderListing(packet.Listings[i]);
                var args = new FellowshipFinderListingEventArgs(packet.ChunkNumber);
                this.ReceiveListing?.Invoke(listing, args);

                if (args.Visible)
                {
                    continue;
                }

                packet.Listings[i] = default;
                needToRewrite = true;
            }

            if (!needToRewrite)
            {
                return;
            }

            Marshal.StructureToPtr(packet, data, false);
        }
    }
}

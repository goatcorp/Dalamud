using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Internal.Gui.Structs;
using Dalamud.Hooking;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class PartyFinderGui : IDisposable {
        #region Events

        public delegate void PartyFinderListingEventDelegate(PartyFinderListing listing, PartyFinderListingEventArgs args);

        /// <summary>
        /// Event fired each time the game receives an individual Party Finder listing. Cannot modify listings but can
        /// hide them.
        /// </summary>
        public event PartyFinderListingEventDelegate ReceiveListing;

        #endregion

        #region Hooks

        private readonly Hook<ReceiveListingDelegate> receiveListingHook;

        #endregion

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate void ReceiveListingDelegate(IntPtr managerPtr, IntPtr data);

        #endregion

        private Dalamud Dalamud { get; }
        private PartyFinderAddressResolver Address { get; }
        private IntPtr Memory { get; }

        public PartyFinderGui(SigScanner scanner, Dalamud dalamud) {
            Dalamud = dalamud;

            Address = new PartyFinderAddressResolver();
            Address.Setup(scanner);

            Memory = Marshal.AllocHGlobal(PartyFinder.PacketInfo.PacketSize);

            this.receiveListingHook = new Hook<ReceiveListingDelegate>(Address.ReceiveListing, new ReceiveListingDelegate(HandleReceiveListingDetour));
        }

        public void Enable() {
            this.receiveListingHook.Enable();
        }

        public void Dispose() {
            this.receiveListingHook.Dispose();
            Marshal.FreeHGlobal(Memory);
        }

        private void HandleReceiveListingDetour(IntPtr managerPtr, IntPtr data) {
            try {
                HandleListingEvents(data);
            } catch (Exception ex) {
                Log.Error(ex, "Exception on ReceiveListing hook.");
            }

            this.receiveListingHook.Original(managerPtr, data);
        }

        private void HandleListingEvents(IntPtr data) {
            var dataPtr = data + 0x10;

            var packet = Marshal.PtrToStructure<PartyFinder.Packet>(dataPtr);

            // rewriting is an expensive operation, so only do it if necessary
            var needToRewrite = false;

            for (var i = 0; i < packet.listings.Length; i++) {
                // these are empty slots that are not shown to the player
                if (packet.listings[i].IsNull()) {
                    continue;
                }

                var listing = new PartyFinderListing(packet.listings[i], Dalamud.Data, Dalamud.SeStringManager);
                var args = new PartyFinderListingEventArgs(packet.batchNumber);
                ReceiveListing?.Invoke(listing, args);

                if (args.Visible) {
                    continue;
                }

                // hide the listing from the player by setting it to a null listing
                packet.listings[i] = new PartyFinder.Listing();
                needToRewrite = true;
            }

            if (!needToRewrite) {
                return;
            }

            // write our struct into the memory (doing this directly crashes the game)
            Marshal.StructureToPtr(packet, Memory, false);

            // copy our new memory over the game's
            unsafe {
                Buffer.MemoryCopy(
                    (void*) Memory,
                    (void*) dataPtr,
                    PartyFinder.PacketInfo.PacketSize,
                    PartyFinder.PacketInfo.PacketSize
                );
            }
        }
    }

    public class PartyFinderListingEventArgs {
        public int BatchNumber { get; }

        public bool Visible { get; set; } = true;

        internal PartyFinderListingEventArgs(int batchNumber) {
            BatchNumber = batchNumber;
        }
    }
}

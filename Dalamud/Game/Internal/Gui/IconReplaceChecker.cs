using System;
using System.Diagnostics;
using Dalamud.Hooking;

namespace Dalamud.Game.Internal.Gui {
    public class IconReplaceChecker {
        private IconReplaceCheckerAddressResolver address;
        private Hook<OnCheckDetour> checkerHook;

        public IconReplaceChecker(ProcessModule module, SigScanner scanner) {
            this.address = new IconReplaceCheckerAddressResolver();
            this.address.Setup(scanner);
            hookChecker();
        }

        private void hookChecker() {
            this.checkerHook = new Hook<OnCheckDetour>(this.address.BaseAddress, (Delegate)new OnCheckDetour(this.HandleChecker), (object)this);
        }

        public void Enable() {
            this.checkerHook.Enable();
        }

        public void Dispose() {
            this.checkerHook.Dispose();
        }

        // I hate this function. This is the dumbest function to exist in the game. Just return 1.
        // Determines which abilities are allowed to have their icons updated.
        private ulong HandleChecker(int actionID) {
            return 1;
        }

        private delegate ulong OnCheckDetour(int actionID);

        public delegate ulong OnCheckDelegate(int actionID);
    }
}

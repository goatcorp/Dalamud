using System;

namespace Dalamud.Game.Gui.ContextMenus
{
    public class ContextMenuAddressResolver : BaseAddressResolver
    {

        private const string SigOpenSubContextMenu = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 86";
        private const string SigContextMenuOpening = "E8 ?? ?? ?? ?? 0F B7 C0 48 83 C4 60";
        private const string SigContextMenuOpened = "48 8B C4 57 41 56 41 57 48 81 EC";
        private const string SigContextMenuItemSelected = "48 89 5C 24 ?? 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9";
        private const string SigSubContextMenuOpening = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 84";
        private const string SigSubContextMenuOpened = "48 8B C4 57 41 55 41 56 48 81 EC";

        public IntPtr OpenSubContextMenuPtr { get; private set; }

        public IntPtr ContextMenuOpeningPtr { get; private set; }

        public IntPtr ContextMenuOpenedPtr { get; private set; }

        public IntPtr ContextMenuItemSelectedPtr { get; private set; }

        public IntPtr SubContextMenuOpeningPtr { get; private set; }

        public IntPtr SubContextMenuOpenedPtr { get; private set; }

        protected override void Setup64Bit(SigScanner scanner)
        {
            this.OpenSubContextMenuPtr = scanner.ScanText(SigOpenSubContextMenu);
            this.ContextMenuOpeningPtr = scanner.ScanText(SigContextMenuOpening);
            this.ContextMenuOpenedPtr = scanner.ScanText(SigContextMenuOpened);
            this.ContextMenuItemSelectedPtr = scanner.ScanText(SigContextMenuItemSelected);
            this.SubContextMenuOpeningPtr = scanner.ScanText(SigSubContextMenuOpening);
            this.SubContextMenuOpenedPtr = scanner.ScanText(SigSubContextMenuOpened);
        }
    }
}

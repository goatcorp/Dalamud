using System;

namespace Dalamud.Game.Gui.ContextMenus
{
    /// <summary>
    /// Address resolver for context menu functions.
    /// </summary>
    public class ContextMenuAddressResolver : BaseAddressResolver
    {
        private const string SomeOpenAddonThing = "E8 ?? ?? ?? ?? 0F B7 C0 48 83 C4 60";
        private const string ContextMenuOpen = "48 8B C4 57 41 56 41 57 48 81 EC";

        private const string ContextMenuSelected =
            "48 89 5C 24 ?? 55 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9";

        private const string ContextMenuEvent66 = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 84";
        private const string SetUpContextSubMenu = "E8 ?? ?? ?? ?? 44 39 A3 ?? ?? ?? ?? 0F 86";
        private const string TitleContextMenuOpen = "48 8B C4 57 41 55 41 56 48 81 EC";
        private const string AtkValueChangeType = "E8 ?? ?? ?? ?? 45 84 F6 48 8D 4C 24";
        private const string AtkValueSetString = "E8 ?? ?? ?? ?? 41 03 ED";
        private const string GetAddonByInternalId = "E8 ?? ?? ?? ?? 8B 6B 20";

        /// <summary>
        /// Gets the ContextMenuChangeTypePtr address.
        /// </summary>
        public IntPtr ContextMenuChangeTypePtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuSetStringPtr address.
        /// </summary>
        public IntPtr ContextMenuSetStringPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuGetAddonPtr address.
        /// </summary>
        public IntPtr ContextMenuGetAddonPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuSetupSubMenuPtr address.
        /// </summary>
        public IntPtr ContextMenuSetupSubMenuPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuOpenAddonPtr address.
        /// </summary>
        public IntPtr ContextMenuOpenAddonPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuOpenPtr address.
        /// </summary>
        public IntPtr ContextMenuOpenPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuSelectedPtr address.
        /// </summary>
        public IntPtr ContextMenuSelectedPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuTitleMenuOpenPtr address.
        /// </summary>
        public IntPtr ContextMenuTitleMenuOpenPtr { get; private set; }

        /// <summary>
        /// Gets the ContextMenuEvent66Ptr address.
        /// </summary>
        public IntPtr ContextMenuEvent66Ptr { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner scanner)
        {
            this.ContextMenuChangeTypePtr = scanner.ScanText(AtkValueChangeType);
            this.ContextMenuSetStringPtr = scanner.ScanText(AtkValueSetString);
            this.ContextMenuGetAddonPtr = scanner.ScanText(GetAddonByInternalId);
            this.ContextMenuSetupSubMenuPtr = scanner.ScanText(SetUpContextSubMenu);
            this.ContextMenuOpenAddonPtr = scanner.ScanText(SomeOpenAddonThing);
            this.ContextMenuOpenPtr = scanner.ScanText(ContextMenuOpen);
            this.ContextMenuSelectedPtr = scanner.ScanText(ContextMenuSelected);
            this.ContextMenuTitleMenuOpenPtr = scanner.ScanText(TitleContextMenuOpen);
            this.ContextMenuEvent66Ptr = scanner.ScanText(ContextMenuEvent66);
        }
    }
}

using System;

using Dalamud.Game.Internal.Gui.Structs;

namespace Dalamud.Game.Internal.Gui.Addons
{
    /// <summary>
    /// This class represents an in-game UI "Addon".
    /// </summary>
    public class Addon
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Addon"/> class.
        /// </summary>
        /// <param name="address">The address of the addon.</param>
        /// <param name="addonStruct">The addon interop data.</param>
        public Addon(IntPtr address, AddonStruct addonStruct)
        {
            this.Address = address;
            this.AddonStruct = addonStruct;
        }

        /// <summary>
        /// Gets the address of the addon.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets the name of the addon.
        /// </summary>
        public string Name => this.AddonStruct.Name;

        /// <summary>
        /// Gets the X position of the addon on screen.
        /// </summary>
        public short X => this.AddonStruct.X;

        /// <summary>
        /// Gets the Y position of the addon on screen.
        /// </summary>
        public short Y => this.AddonStruct.Y;

        /// <summary>
        /// Gets the scale of the addon.
        /// </summary>
        public float Scale => this.AddonStruct.Scale;

        /// <summary>
        /// Gets the width of the addon. This may include non-visible parts.
        /// </summary>
        public unsafe float Width => this.AddonStruct.RootNode->Width * this.Scale;

        /// <summary>
        /// Gets the height of the addon. This may include non-visible parts.
        /// </summary>
        public unsafe float Height => this.AddonStruct.RootNode->Height * this.Scale;

        /// <summary>
        /// Gets a value indicating whether the addon is visible.
        /// </summary>
        public bool Visible => (this.AddonStruct.Flags & 0x20) == 0x20;

        /// <summary>
        /// Gets the addon interop data.
        /// </summary>
        protected Structs.AddonStruct AddonStruct { get; }
    }
}

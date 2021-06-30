using System;

namespace Dalamud.Game.Internal.Gui.Addon
{
    /// <summary>
    /// This class represents an in-game UI "Addon".
    /// </summary>
    public class Addon
    {
        /// <summary>
        /// The address of the addon.
        /// </summary>
        public IntPtr Address;

        /// <summary>
        /// The addon interop data.
        /// </summary>
        protected Structs.Addon addonStruct;

        /// <summary>
        /// Initializes a new instance of the <see cref="Addon"/> class.
        /// </summary>
        /// <param name="address">The address of the addon.</param>
        /// <param name="addonStruct">The addon interop data.</param>
        public Addon(IntPtr address, Structs.Addon addonStruct)
        {
            this.Address = address;
            this.addonStruct = addonStruct;
        }

        /// <summary>
        /// Gets the name of the addon.
        /// </summary>
        public string Name => this.addonStruct.Name;

        /// <summary>
        /// Gets the X position of the addon on screen.
        /// </summary>
        public short X => this.addonStruct.X;

        /// <summary>
        /// Gets the Y position of the addon on screen.
        /// </summary>
        public short Y => this.addonStruct.Y;

        /// <summary>
        /// Gets the scale of the addon.
        /// </summary>
        public float Scale => this.addonStruct.Scale;

        /// <summary>
        /// Gets the width of the addon. This may include non-visible parts.
        /// </summary>
        public unsafe float Width => this.addonStruct.RootNode->Width * this.Scale;

        /// <summary>
        /// Gets the height of the addon. This may include non-visible parts.
        /// </summary>
        public unsafe float Height => this.addonStruct.RootNode->Height * this.Scale;

        /// <summary>
        /// Gets a value indicating whether the addon is visible.
        /// </summary>
        public bool Visible => (this.addonStruct.Flags & 0x20) == 0x20;
    }
}

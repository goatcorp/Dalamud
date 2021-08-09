using System;

using Dalamud.Memory;

namespace Dalamud.Game.Gui.Addons
{
    /// <summary>
    /// This class represents an in-game UI "Addon".
    /// </summary>
    public unsafe class Addon
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Addon"/> class.
        /// </summary>
        /// <param name="address">The address of the addon.</param>
        public Addon(IntPtr address)
        {
            this.Address = address;
        }

        /// <summary>
        /// Gets the address of the addon.
        /// </summary>
        public IntPtr Address { get; }

        /// <summary>
        /// Gets the name of the addon.
        /// </summary>
        public string Name => MemoryHelper.ReadString((IntPtr)this.Struct->Name, 0x20);

        /// <summary>
        /// Gets the X position of the addon on screen.
        /// </summary>
        public short X => this.Struct->X;

        /// <summary>
        /// Gets the Y position of the addon on screen.
        /// </summary>
        public short Y => this.Struct->Y;

        /// <summary>
        /// Gets the scale of the addon.
        /// </summary>
        public float Scale => this.Struct->Scale;

        /// <summary>
        /// Gets the width of the addon. This may include non-visible parts.
        /// </summary>
        public unsafe float Width => this.Struct->RootNode->Width * this.Scale;

        /// <summary>
        /// Gets the height of the addon. This may include non-visible parts.
        /// </summary>
        public unsafe float Height => this.Struct->RootNode->Height * this.Scale;

        /// <summary>
        /// Gets a value indicating whether the addon is visible.
        /// </summary>
        public bool Visible => this.Struct->IsVisible;

        private FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase* Struct => (FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)this.Address;
    }
}

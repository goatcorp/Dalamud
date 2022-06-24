using System;
using System.Collections.Generic;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using ImGuiScene;

namespace Dalamud.Interface
{
    /// <summary>
    /// Class responsible for managing elements in the title screen menu.
    /// </summary>
    [PluginInterface]
    [InterfaceVersion("1.0")]
    public class TitleScreenMenu : IEarlyLoadableServiceObject
    {
        /// <summary>
        /// Gets the texture size needed for title screen menu logos.
        /// </summary>
        internal const uint TextureSize = 64;

        private readonly List<TitleScreenMenuEntry> entries = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleScreenMenu"/> class.
        /// </summary>
        /// <param name="tag">Tag.</param>
        private TitleScreenMenu(ServiceManager.Tag tag)
        {
        }

        /// <summary>
        /// Gets the list of entries in the title screen menu.
        /// </summary>
        public IReadOnlyList<TitleScreenMenuEntry> Entries => this.entries;

        /// <summary>
        /// Adds a new entry to the title screen menu.
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="texture">The texture to show.</param>
        /// <param name="onTriggered">The action to execute when the option is selected.</param>
        /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
        /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
        public TitleScreenMenuEntry AddEntry(string text, TextureWrap texture, Action onTriggered)
        {
            if (texture.Height != TextureSize || texture.Width != TextureSize)
            {
                throw new ArgumentException("Texture must be 64x64");
            }

            var entry = new TitleScreenMenuEntry(text, texture, onTriggered);
            this.entries.Add(entry);
            return entry;
        }

        /// <summary>
        /// Remove an entry from the title screen menu.
        /// </summary>
        /// <param name="entry">The entry to remove.</param>
        public void RemoveEntry(TitleScreenMenuEntry entry) => this.entries.Remove(entry);

        /// <summary>
        /// Class representing an entry in the title screen menu.
        /// </summary>
        public class TitleScreenMenuEntry
        {
            private readonly Action onTriggered;

            /// <summary>
            /// Initializes a new instance of the <see cref="TitleScreenMenuEntry"/> class.
            /// </summary>
            /// <param name="text">The text to show.</param>
            /// <param name="texture">The texture to show.</param>
            /// <param name="onTriggered">The action to execute when the option is selected.</param>
            internal TitleScreenMenuEntry(string text, TextureWrap texture, Action onTriggered)
            {
                this.Name = text;
                this.Texture = texture;
                this.onTriggered = onTriggered;
            }

            /// <summary>
            /// Gets or sets the name of this entry.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the texture of this entry.
            /// </summary>
            public TextureWrap Texture { get; set; }

            /// <summary>
            /// Gets the internal ID of this entry.
            /// </summary>
            internal Guid Id { get; init; } = Guid.NewGuid();

            /// <summary>
            /// Trigger the action associated with this entry.
            /// </summary>
            internal void Trigger()
            {
                this.onTriggered();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using ImGuiScene;

namespace Dalamud.Interface;

/// <summary>
/// Class responsible for managing elements in the title screen menu.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public class TitleScreenMenu : IServiceType
{
    /// <summary>
    /// Gets the texture size needed for title screen menu logos.
    /// </summary>
    internal const uint TextureSize = 64;

    private readonly List<TitleScreenMenuEntry> entries = new();

    [ServiceManager.ServiceConstructor]
    private TitleScreenMenu()
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

        lock (this.entries)
        {
            var entriesOfAssembly = this.entries.Where(x => x.CallingAssembly == Assembly.GetCallingAssembly()).ToList();
            var priority = entriesOfAssembly.Any()
                               ? unchecked(entriesOfAssembly.Select(x => x.Priority).Max() + 1)
                               : 0;
            var entry = new TitleScreenMenuEntry(Assembly.GetCallingAssembly(), priority, text, texture, onTriggered);
            var i = this.entries.BinarySearch(entry);
            if (i < 0)
                i = ~i;
            this.entries.Insert(i, entry);
            return entry;
        }
    }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="priority">Priority of the entry.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    public TitleScreenMenuEntry AddEntry(ulong priority, string text, TextureWrap texture, Action onTriggered)
    {
        if (texture.Height != TextureSize || texture.Width != TextureSize)
        {
            throw new ArgumentException("Texture must be 64x64");
        }

        lock (this.entries)
        {
            var entry = new TitleScreenMenuEntry(Assembly.GetCallingAssembly(), priority, text, texture, onTriggered);
            var i = this.entries.BinarySearch(entry);
            if (i < 0)
                i = ~i;
            this.entries.Insert(i, entry);
            return entry;
        }
    }

    /// <summary>
    /// Remove an entry from the title screen menu.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    public void RemoveEntry(TitleScreenMenuEntry entry)
    {
        lock (this.entries)
        {
            this.entries.Remove(entry);
        }
    }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="priority">Priority of the entry.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    internal TitleScreenMenuEntry AddEntryCore(ulong priority, string text, TextureWrap texture, Action onTriggered)
    {
        if (texture.Height != TextureSize || texture.Width != TextureSize)
        {
            throw new ArgumentException("Texture must be 64x64");
        }

        lock (this.entries)
        {
            var entry = new TitleScreenMenuEntry(null, priority, text, texture, onTriggered)
            {
                IsInternal = true,
            };
            this.entries.Add(entry);
            return entry;
        }
    }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    internal TitleScreenMenuEntry AddEntryCore(string text, TextureWrap texture, Action onTriggered)
    {
        if (texture.Height != TextureSize || texture.Width != TextureSize)
        {
            throw new ArgumentException("Texture must be 64x64");
        }

        lock (this.entries)
        {
            var entriesOfAssembly = this.entries.Where(x => x.CallingAssembly == null).ToList();
            var priority = entriesOfAssembly.Any()
                               ? unchecked(entriesOfAssembly.Select(x => x.Priority).Max() + 1)
                               : 0;
            var entry = new TitleScreenMenuEntry(null, priority, text, texture, onTriggered)
            {
                IsInternal = true,
            };
            this.entries.Add(entry);
            return entry;
        }
    }

    /// <summary>
    /// Class representing an entry in the title screen menu.
    /// </summary>
    public class TitleScreenMenuEntry : IComparable<TitleScreenMenuEntry>
    {
        private readonly Action onTriggered;

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleScreenMenuEntry"/> class.
        /// </summary>
        /// <param name="callingAssembly">The calling assembly.</param>
        /// <param name="priority">The priority of this entry.</param>
        /// <param name="text">The text to show.</param>
        /// <param name="texture">The texture to show.</param>
        /// <param name="onTriggered">The action to execute when the option is selected.</param>
        internal TitleScreenMenuEntry(Assembly? callingAssembly, ulong priority, string text, TextureWrap texture, Action onTriggered)
        {
            this.CallingAssembly = callingAssembly;
            this.Priority = priority;
            this.Name = text;
            this.Texture = texture;
            this.onTriggered = onTriggered;
        }

        /// <summary>
        /// Gets the priority of this entry.
        /// </summary>
        public ulong Priority { get; init; }

        /// <summary>
        /// Gets or sets the name of this entry.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the texture of this entry.
        /// </summary>
        public TextureWrap Texture { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether or not this entry is internal.
        /// </summary>
        internal bool IsInternal { get; set; }

        /// <summary>
        /// Gets the calling assembly of this entry.
        /// </summary>
        internal Assembly? CallingAssembly { get; init; }

        /// <summary>
        /// Gets the internal ID of this entry.
        /// </summary>
        internal Guid Id { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public int CompareTo(TitleScreenMenuEntry? other)
        {
            if (other == null)
                return 1;
            if (this.CallingAssembly != other.CallingAssembly)
            {
                if (this.CallingAssembly == null && other.CallingAssembly == null)
                    return 0;
                if (this.CallingAssembly == null && other.CallingAssembly != null)
                    return -1;
                if (this.CallingAssembly != null && other.CallingAssembly == null)
                    return 1;
                return string.Compare(
                    this.CallingAssembly!.FullName!,
                    other.CallingAssembly!.FullName!,
                    StringComparison.CurrentCultureIgnoreCase);
            }

            if (this.Priority != other.Priority)
                return this.Priority.CompareTo(other.Priority);
            if (this.Name != other.Name)
                return string.Compare(this.Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
            return string.Compare(this.Name, other.Name, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Trigger the action associated with this entry.
        /// </summary>
        internal void Trigger()
        {
            this.onTriggered();
        }
    }
}

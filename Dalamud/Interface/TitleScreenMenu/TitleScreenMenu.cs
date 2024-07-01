using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

namespace Dalamud.Interface;

using Textures;

/// <summary>
/// Class responsible for managing elements in the title screen menu.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class TitleScreenMenu : IServiceType, ITitleScreenMenu
{
    /// <summary>
    /// Gets the texture size needed for title screen menu logos.
    /// </summary>
    internal const uint TextureSize = 64;

    private readonly List<TitleScreenMenuEntry> entries = new();
    private TitleScreenMenuEntry[]? entriesView;

    [ServiceManager.ServiceConstructor]
    private TitleScreenMenu()
    {
    }

    /// <summary>
    /// Event to be called when the entry list has been changed.
    /// </summary>
    internal event Action? EntryListChange;

    /// <inheritdoc/>
    public IReadOnlyList<IReadOnlyTitleScreenMenuEntry> Entries
    {
        get
        {
            lock (this.entries)
            {
                if (!this.entries.Any())
                    return Array.Empty<TitleScreenMenuEntry>();

                return this.entriesView ??= this.entries.OrderByDescending(x => x.IsInternal).ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the list of entries in the title screen menu.
    /// </summary>
    public IReadOnlyList<ITitleScreenMenuEntry> PluginEntries
    {
        get
        {
            lock (this.entries)
            {
                if (!this.entries.Any())
                    return Array.Empty<TitleScreenMenuEntry>();

                return this.entriesView ??= this.entries.OrderByDescending(x => x.IsInternal).ToArray();
            }
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
    public ITitleScreenMenuEntry AddPluginEntry(string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        TitleScreenMenuEntry entry;
        lock (this.entries)
        {
            var entriesOfAssembly = this.entries.Where(x => x.CallingAssembly == Assembly.GetCallingAssembly()).ToList();
            var priority = entriesOfAssembly.Any()
                               ? unchecked(entriesOfAssembly.Select(x => x.Priority).Max() + 1)
                               : 0;
            entry = new(Assembly.GetCallingAssembly(), priority, text, texture, onTriggered);
            var i = this.entries.BinarySearch(entry);
            if (i < 0)
                i = ~i;
            this.entries.Insert(i, entry);
            this.entriesView = null;
        }

        this.EntryListChange?.InvokeSafely();
        return entry;
    }

    /// <inheritdoc/>
    public IReadOnlyTitleScreenMenuEntry AddEntry(string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        return this.AddPluginEntry(text, texture, onTriggered);
    }

    /// <inheritdoc/>
    public IReadOnlyTitleScreenMenuEntry AddEntry(ulong priority, string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        return this.AddPluginEntry(priority, text, texture, onTriggered);
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
    public ITitleScreenMenuEntry AddPluginEntry(ulong priority, string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        TitleScreenMenuEntry entry;
        lock (this.entries)
        {
            entry = new(Assembly.GetCallingAssembly(), priority, text, texture, onTriggered);
            var i = this.entries.BinarySearch(entry);
            if (i < 0)
                i = ~i;
            this.entries.Insert(i, entry);
            this.entriesView = null;
        }

        this.EntryListChange?.InvokeSafely();
        return entry;
    }

    /// <inheritdoc/>
    public void RemoveEntry(IReadOnlyTitleScreenMenuEntry entry)
    {
        lock (this.entries)
        {
            this.entries.RemoveAll(pluginEntry => pluginEntry == entry);
            this.entriesView = null;
        }

        this.EntryListChange?.InvokeSafely();
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
    internal TitleScreenMenuEntry AddEntryCore(ulong priority, string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        TitleScreenMenuEntry entry;
        lock (this.entries)
        {
            entry = new(null, priority, text, texture, onTriggered)
            {
                IsInternal = true,
            };
            this.entries.Add(entry);
            this.entriesView = null;
        }

        this.EntryListChange?.InvokeSafely();
        return entry;
    }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <param name="showConditionKeys">The keys that have to be held to display the menu.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    internal TitleScreenMenuEntry AddEntryCore(
        string text,
        ISharedImmediateTexture texture,
        Action onTriggered,
        params VirtualKey[] showConditionKeys)
    {
        TitleScreenMenuEntry entry;
        lock (this.entries)
        {
            var entriesOfAssembly = this.entries.Where(x => x.CallingAssembly == null).ToList();
            var priority = entriesOfAssembly.Any()
                               ? unchecked(entriesOfAssembly.Select(x => x.Priority).Max() + 1)
                               : 0;
            entry = new(null, priority, text, texture, onTriggered, showConditionKeys)
            {
                IsInternal = true,
            };
            this.entries.Add(entry);
            this.entriesView = null;
        }

        this.EntryListChange?.InvokeSafely();
        return entry;
    }
}

/// <summary>
/// Plugin-scoped version of a TitleScreenMenu service.
/// </summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ITitleScreenMenu>]
#pragma warning restore SA1015
internal class TitleScreenMenuPluginScoped : IInternalDisposableService, ITitleScreenMenu
{
    [ServiceManager.ServiceDependency]
    private readonly TitleScreenMenu titleScreenMenuService = Service<TitleScreenMenu>.Get();
    
    private readonly List<IReadOnlyTitleScreenMenuEntry> pluginEntries = new();

    /// <inheritdoc/>
    public IReadOnlyList<IReadOnlyTitleScreenMenuEntry>? Entries => this.titleScreenMenuService.Entries;

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        foreach (var entry in this.pluginEntries)
        {
            this.titleScreenMenuService.RemoveEntry(entry);
        }
    }
    
    /// <inheritdoc/>
    public IReadOnlyTitleScreenMenuEntry AddEntry(string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        var entry = this.titleScreenMenuService.AddPluginEntry(text, texture, onTriggered);
        this.pluginEntries.Add(entry);

        return entry;
    }
    
    /// <inheritdoc/>
    public IReadOnlyTitleScreenMenuEntry AddEntry(ulong priority, string text, ISharedImmediateTexture texture, Action onTriggered)
    {
        var entry = this.titleScreenMenuService.AddPluginEntry(priority, text, texture, onTriggered);
        this.pluginEntries.Add(entry);

        return entry;
    }
    
    /// <inheritdoc/>
    public void RemoveEntry(IReadOnlyTitleScreenMenuEntry entry)
    {
        this.pluginEntries.Remove(entry);
        this.titleScreenMenuService.RemoveEntry(entry);
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Interface.Internal;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;
using ImGuiScene;

namespace Dalamud.Interface;

/// <summary>
/// Class responsible for managing elements in the title screen menu.
/// </summary>
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
internal class TitleScreenMenu : IServiceType, ITitleScreenMenu
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

    /// <inheritdoc/>
    public IReadOnlyList<TitleScreenMenuEntry> Entries => this.entries;

    /// <inheritdoc/>
    public TitleScreenMenuEntry AddEntry(string text, IDalamudTextureWrap texture, Action onTriggered)
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

    /// <inheritdoc/>
    public TitleScreenMenuEntry AddEntry(ulong priority, string text, IDalamudTextureWrap texture, Action onTriggered)
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

    /// <inheritdoc/>
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
    internal TitleScreenMenuEntry AddEntryCore(ulong priority, string text, IDalamudTextureWrap texture, Action onTriggered)
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
    internal TitleScreenMenuEntry AddEntryCore(string text, IDalamudTextureWrap texture, Action onTriggered)
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
}

/// <summary>
/// Plugin-scoped version of a TitleScreenMenu service.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<ITitleScreenMenu>]
#pragma warning restore SA1015
internal class TitleScreenMenuPluginScoped : IDisposable, IServiceType, ITitleScreenMenu
{
    [ServiceManager.ServiceDependency]
    private readonly TitleScreenMenu titleScreenMenuService = Service<TitleScreenMenu>.Get();
    
    private readonly List<TitleScreenMenuEntry> pluginEntries = new();

    /// <inheritdoc/>
    public IReadOnlyList<TitleScreenMenuEntry>? Entries => this.titleScreenMenuService.Entries;

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var entry in this.pluginEntries)
        {
            this.titleScreenMenuService.RemoveEntry(entry);
        }
    }
    
    /// <inheritdoc/>
    public TitleScreenMenuEntry AddEntry(string text, IDalamudTextureWrap texture, Action onTriggered)
    {
        var entry = this.titleScreenMenuService.AddEntry(text, texture, onTriggered);
        this.pluginEntries.Add(entry);

        return entry;
    }
    
    /// <inheritdoc/>
    public TitleScreenMenuEntry AddEntry(ulong priority, string text, IDalamudTextureWrap texture, Action onTriggered)
    {
        var entry = this.titleScreenMenuService.AddEntry(priority, text, texture, onTriggered);
        this.pluginEntries.Add(entry);

        return entry;
    }
    
    /// <inheritdoc/>
    public void RemoveEntry(TitleScreenMenuEntry entry)
    {
        this.pluginEntries.Remove(entry);
        this.titleScreenMenuService.RemoveEntry(entry);
    }
}

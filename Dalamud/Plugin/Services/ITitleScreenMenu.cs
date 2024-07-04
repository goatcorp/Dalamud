using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace Dalamud.Plugin.Services;

using Interface.Textures;

/// <summary>
/// Interface for class responsible for managing elements in the title screen menu.
/// </summary>
public interface ITitleScreenMenu
{
    /// <summary>
    /// Gets the list of read only entries in the title screen menu.
    /// </summary>
    public IReadOnlyList<IReadOnlyTitleScreenMenuEntry> Entries { get; }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show. The texture must be 64x64 or the entry will not draw.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="IReadOnlyTitleScreenMenuEntry"/> object that can be reference the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    public IReadOnlyTitleScreenMenuEntry AddEntry(string text, ISharedImmediateTexture texture, Action onTriggered);

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="priority">Priority of the entry.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show. The texture must be 64x64 or the entry will not draw.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="IReadOnlyTitleScreenMenuEntry"/> object that can be used to reference the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    public IReadOnlyTitleScreenMenuEntry AddEntry(ulong priority, string text, ISharedImmediateTexture texture, Action onTriggered);

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show. The texture must be 64x64 or the entry will not draw. Please use ISharedImmediateTexture or a ForwardingSharedImmediateTexture if possible.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="IReadOnlyTitleScreenMenuEntry"/> object that can be reference the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    [Obsolete("Will be removed in API11")]
    public IReadOnlyTitleScreenMenuEntry AddEntry(string text, IDalamudTextureWrap texture, Action onTriggered)
    {
        return this.AddEntry(text, new ForwardingSharedImmediateTexture(texture), onTriggered);
    }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="priority">Priority of the entry.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show. The texture must be 64x64 or the entry will not draw. Please use ISharedImmediateTexture or a ForwardingSharedImmediateTexture if possible.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="IReadOnlyTitleScreenMenuEntry"/> object that can be used to reference the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    [Obsolete("Will be removed in API11")]
    public IReadOnlyTitleScreenMenuEntry AddEntry(ulong priority, string text, IDalamudTextureWrap texture, Action onTriggered)
    {
        return this.AddEntry(priority, text, new ForwardingSharedImmediateTexture(texture), onTriggered);
    }

    /// <summary>
    /// Remove an entry from the title screen menu.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    public void RemoveEntry(IReadOnlyTitleScreenMenuEntry entry);
}

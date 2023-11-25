using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.Internal;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Interface for class responsible for managing elements in the title screen menu.
/// </summary>
public interface ITitleScreenMenu
{
    /// <summary>
    /// Gets the list of entries in the title screen menu.
    /// </summary>
    public IReadOnlyList<TitleScreenMenuEntry> Entries { get; }

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    public TitleScreenMenuEntry AddEntry(string text, IDalamudTextureWrap texture, Action onTriggered);

    /// <summary>
    /// Adds a new entry to the title screen menu.
    /// </summary>
    /// <param name="priority">Priority of the entry.</param>
    /// <param name="text">The text to show.</param>
    /// <param name="texture">The texture to show.</param>
    /// <param name="onTriggered">The action to execute when the option is selected.</param>
    /// <returns>A <see cref="TitleScreenMenu"/> object that can be used to manage the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the texture provided does not match the required resolution(64x64).</exception>
    public TitleScreenMenuEntry AddEntry(ulong priority, string text, IDalamudTextureWrap texture, Action onTriggered);

    /// <summary>
    /// Remove an entry from the title screen menu.
    /// </summary>
    /// <param name="entry">The entry to remove.</param>
    public void RemoveEntry(TitleScreenMenuEntry entry);
}

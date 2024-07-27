using System.Collections.Generic;

using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Class used to interface with the server info bar.
/// </summary>
public interface IDtrBar
{
    /// <summary>
    /// Gets a read-only copy of the list of all DTR bar entries.
    /// </summary>
    /// <remarks>If the list changes due to changes in order or insertion/removal, then this property will return a
    /// completely new object on getter invocation. The returned object is safe to use from any thread, and will not
    /// change.</remarks>
    IReadOnlyList<IReadOnlyDtrBarEntry> Entries { get; }

    /// <summary>
    /// Get a DTR bar entry.
    /// This allows you to add your own text, and users to sort it.
    /// </summary>
    /// <param name="title">A user-friendly name for sorting.</param>
    /// <param name="text">The text the entry shows.</param>
    /// <returns>The entry object used to update, hide and remove the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when an entry with the specified title exists.</exception>
    IDtrBarEntry Get(string title, SeString? text = null);

    /// <summary>
    /// Removes a DTR bar entry from the system.
    /// </summary>
    /// <param name="title">Title of the entry to remove.</param>
    /// <remarks>Remove operation is not immediate. Attempts to call <see cref="Get"/> immediately after calling this
    /// function may fail.</remarks>
    void Remove(string title);
}

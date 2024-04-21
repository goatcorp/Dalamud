using System.Collections.Generic;

using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Class used to interface with the server info bar.
/// </summary>
public interface IDtrBar
{
    /// <summary>
    /// Gets a read-only list of all DTR bar entries.
    /// </summary>
    public IReadOnlyList<IReadOnlyDtrBarEntry> Entries { get; }
    
    /// <summary>
    /// Get a DTR bar entry.
    /// This allows you to add your own text, and users to sort it.
    /// </summary>
    /// <param name="title">A user-friendly name for sorting.</param>
    /// <param name="text">The text the entry shows.</param>
    /// <returns>The entry object used to update, hide and remove the entry.</returns>
    /// <exception cref="ArgumentException">Thrown when an entry with the specified title exists.</exception>
    [Api10ToDo("Return IDtrBarEntry instead of DtrBarEntry")]
    public DtrBarEntry Get(string title, SeString? text = null);

    /// <summary>
    /// Removes a DTR bar entry from the system.
    /// </summary>
    /// <param name="title">Title of the entry to remove.</param>
    public void Remove(string title);
}

using Dalamud.Game.Tooltip.StyleOptions;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Tooltip.Entries;

/// <summary>
/// Internal class representing the data from a plugin to add a string entry.
/// </summary>
internal class StringTooltipEntry : TooltipEntry
{
    /// <summary>
    /// Gets or sets the string.
    /// </summary>
    public ReadOnlySeString String { get; set; }

    /// <summary>
    /// Gets or sets the text node options.
    /// </summary>
    public TextOptions Options { get; set; } = new();
}

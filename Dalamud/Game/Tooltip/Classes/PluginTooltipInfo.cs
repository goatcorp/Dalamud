using Dalamud.Game.Tooltip.Nodes;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Tooltip.Classes;

/// <summary>
/// Object representing a plugin's information, and a built tooltip node.
/// </summary>
internal class PluginTooltipInfo
{
    /// <summary>
    /// Gets or sets the name of the plugin generating this entry.
    /// </summary>
    public required ReadOnlySeString SourcePluginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tooltip node to be shown for this plugin.
    /// </summary>
    public required CustomTooltipNode TooltipNode { get; set; }
}

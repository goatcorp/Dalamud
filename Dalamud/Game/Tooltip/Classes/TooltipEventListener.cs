using Dalamud.Plugin.Services;

using Lumina.Text.ReadOnly;

namespace Dalamud.Game.Tooltip.Classes;

/// <summary>
/// Object representing a tooltip event listeners.
/// </summary>
internal class TooltipEventListener
{
    /// <summary>
    /// Gets the name of the plugin that requested this listener.
    /// </summary>
    public required ReadOnlySeString SourcePluginName { get; init; }

    /// <summary>
    /// Gets the delegate to the event listeners callback.
    /// </summary>
    public required ITooltip.TooltipChanged ListenerDelegate { get; init; }

    /// <summary>
    /// Gets the tooltip type for this listener.
    /// </summary>
    public required TooltipType TooltipType { get; init; }
}

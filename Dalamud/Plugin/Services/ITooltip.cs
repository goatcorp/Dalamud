using Dalamud.Game.Tooltip.Classes;
using Dalamud.Game.Tooltip.TooltipArgTypes;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Allows you to add an edit various displayed data for tooltips.
/// </summary>
public interface ITooltip : IDalamudService
{
    /// <summary>
    /// Delegate containing information about the changed tooltip, and access to TooltipArgs
    /// for building custom tooltips.
    /// </summary>
    /// <param name="type">The tooltip type that triggered this callback.</param>
    /// <param name="args">The args object with tooltip info and functions for adding custom tooltips.</param>
    delegate void TooltipChanged(TooltipType type, TooltipArgs args);

    /// <summary>
    /// Registers a listener for tooltip events.
    /// </summary>
    /// <remarks>
    /// Args object contains functionality for creating custom tooltips.
    /// </remarks>
    /// <param name="type">Tooltip type to listen for.</param>
    /// <param name="tooltipChangedDelegate">Delegate that is invoked when a tooltip of the specified type changes.</param>
    void RegisterListener(TooltipType type, TooltipChanged tooltipChangedDelegate);

    /// <summary>
    /// Unregisters a listener from tooltip events.
    /// </summary>
    /// <param name="type">Tooltip type to no longer listen for.</param>
    /// <param name="tooltipChangedDelegate">The delegate to unregister.</param>
    void UnregisterListener(TooltipType type, TooltipChanged tooltipChangedDelegate);

    /// <summary>
    /// Unregisters all listeners for the tooltip type.
    /// </summary>
    /// <param name="type">Tooltip type to no longer listen for.</param>
    void UnregisterListener(TooltipType type);

    /// <summary>
    /// Unregisters the delegate from all listeners.
    /// </summary>
    /// <param name="tooltipChangedDelegate">Delegate to unregister.</param>
    void UnregisterListener(TooltipChanged tooltipChangedDelegate);

    /// <summary>
    /// Unregisters multiple delegates from all listeners.
    /// </summary>
    /// <param name="delegates">Delegates to unregister.</param>
    void UnregisterListener(params TooltipChanged[] delegates);
}

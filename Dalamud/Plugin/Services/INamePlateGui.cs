using System.Collections.Generic;

using Dalamud.Game.Gui.NamePlate;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Class used to modify the data used when rendering nameplates.
/// </summary>
public interface INamePlateGui : IDalamudService
{
    /// <summary>
    /// The delegate used for receiving nameplate update events.
    /// </summary>
    /// <param name="context">An object containing information about the pending data update.</param>
    /// <param name="handlers>">A list of handlers used for updating nameplate data.</param>
    delegate void PlateUpdateDelegate(
        INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers);

    /// <summary>
    /// An event which fires when nameplate data is updated and at least one nameplate has important updates. The
    /// subscriber is provided with a list of handlers for nameplates with important updates.
    /// </summary>
    /// <remarks>
    /// Fires after <see cref="DataUpdate"/>.
    /// </remarks>
    event PlateUpdateDelegate? NamePlateUpdate;

    /// <summary>
    /// An event which fires after nameplate data is updated and at least one nameplate had important updates. The
    /// subscriber is provided with a list of handlers for nameplates with important updates.
    /// </summary>
    /// <remarks>
    /// Fires before <see cref="PostDataUpdate"/>.
    /// </remarks>
    event PlateUpdateDelegate? PostNamePlateUpdate;

    /// <summary>
    /// An event which fires when nameplate data is updated. The subscriber is provided with a list of handlers for all
    /// nameplates.
    /// </summary>
    /// <remarks>
    /// This event is likely to fire every frame even when no nameplates are actually updated, so in most cases
    /// <see cref="NamePlateUpdate"/> is preferred. Fires before <see cref="NamePlateUpdate"/>.
    /// </remarks>
    event PlateUpdateDelegate? DataUpdate;

    /// <summary>
    /// An event which fires after nameplate data is updated. The subscriber is provided with a list of handlers for all
    /// nameplates.
    /// </summary>
    /// <remarks>
    /// This event is likely to fire every frame even when no nameplates are actually updated, so in most cases
    /// <see cref="NamePlateUpdate"/> is preferred. Fires after <see cref="PostNamePlateUpdate"/>.
    /// </remarks>
    event PlateUpdateDelegate? PostDataUpdate;

    /// <summary>
    /// Requests that all nameplates should be redrawn on the following frame.
    /// </summary>
    /// <remarks>
    /// This causes extra work for the game, and should not need to be called every frame. However, it is acceptable to
    /// call frequently when needed (e.g. in response to a manual settings change by the user) or when necessary (e.g.
    /// after a change of zone, party type, etc.).
    /// </remarks>
    void RequestRedraw();
}

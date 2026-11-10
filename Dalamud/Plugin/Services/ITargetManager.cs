using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Get and set various kinds of targets for the player.
/// </summary>
public interface ITargetManager : IDalamudService
{
    /// <summary>
    /// Gets or sets the current target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? Target { get; set; }

    /// <summary>
    /// Gets or sets the mouseover target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? MouseOverTarget { get; set; }

    /// <summary>
    /// Gets or sets the focus target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? FocusTarget { get; set; }

    /// <summary>
    /// Gets or sets the previous target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? PreviousTarget { get; set; }

    /// <summary>
    /// Gets or sets the soft target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? SoftTarget { get; set; }

    /// <summary>
    /// Gets or sets the gpose target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? GPoseTarget { get; set; }

    /// <summary>
    /// Gets or sets the mouseover nameplate target.
    /// Set to null to clear the target.
    /// </summary>
    IGameObject? MouseOverNameplateTarget { get; set; }
}

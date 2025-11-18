using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

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
    public IGameObject? Target { get; set; }

    /// <summary>
    /// Gets or sets the mouseover target.
    /// Set to null to clear the target.
    /// </summary>
    public IGameObject? MouseOverTarget { get; set; }

    /// <summary>
    /// Gets or sets the focus target.
    /// Set to null to clear the target.
    /// </summary>
    public IGameObject? FocusTarget { get; set; }

    /// <summary>
    /// Gets or sets the previous target.
    /// Set to null to clear the target.
    /// </summary>
    public IGameObject? PreviousTarget { get; set; }

    /// <summary>
    /// Gets or sets the soft target.
    /// Set to null to clear the target.
    /// </summary>
    public IGameObject? SoftTarget { get; set; }

    /// <summary>
    /// Gets or sets the gpose target.
    /// Set to null to clear the target.
    /// </summary>
    public IGameObject? GPoseTarget { get; set; }

    /// <summary>
    /// Gets or sets the mouseover nameplate target.
    /// Set to null to clear the target.
    /// </summary>
    public IGameObject? MouseOverNameplateTarget { get; set; }
}

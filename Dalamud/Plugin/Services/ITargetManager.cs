using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.ClientState.Objects;

/// <summary>
/// Get and set various kinds of targets for the player.
/// </summary>
public interface ITargetManager
{
    /// <summary>
    /// Gets the address of the target manager.
    /// </summary>
    public nint Address { get; }

    /// <summary>
    /// Gets or sets the current target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? Target { get; set; }

    /// <summary>
    /// Gets or sets the mouseover target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? MouseOverTarget { get; set; }

    /// <summary>
    /// Gets or sets the focus target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? FocusTarget { get; set; }

    /// <summary>
    /// Gets or sets the previous target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? PreviousTarget { get; set; }

    /// <summary>
    /// Gets or sets the soft target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? SoftTarget { get; set; }
    
    /// <summary>
    /// Gets or sets the gpose target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? GPoseTarget { get; set; }
    
    /// <summary>
    /// Gets or sets the mouseover nameplate target.
    /// Set to null to clear the target.
    /// </summary>
    public GameObject? MouseOverNameplateTarget { get; set; }
}

using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Plugin.Services;

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
    /// </summary>
    public GameObject? Target { get; set; }
    
    /// <summary>
    /// Gets or sets the mouseover target.
    /// </summary>
    public GameObject? MouseOverTarget { get; set; }
    
    /// <summary>
    /// Gets or sets the focus target.
    /// </summary>
    public GameObject? FocusTarget { get; set; }
    
    /// <summary>
    /// Gets or sets the previous target.
    /// </summary>
    public GameObject? PreviousTarget { get; set; }
    
    /// <summary>
    /// Gets or sets the soft target.
    /// </summary>
    public GameObject? SoftTarget { get; set; }
}

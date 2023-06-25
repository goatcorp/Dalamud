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

    /// <summary>
    /// Sets the current target.
    /// </summary>
    /// <param name="gameObject">GameObject to target.</param>
    public void SetTarget(GameObject? gameObject);

    /// <summary>
    /// Sets the mouseover target.
    /// </summary>
    /// <param name="gameObject">GameObject to target.</param>
    public void SetMouseOverTarget(GameObject? gameObject);

    /// <summary>
    /// Sets the focus target.
    /// </summary>
    /// <param name="gameObject">GameObject to target.</param>
    public void SetFocusTarget(GameObject? gameObject);

    /// <summary>
    /// Sets the previous target.
    /// </summary>
    /// <param name="gameObject">GameObject to target.</param>
    public void SetPreviousTarget(GameObject? gameObject);

    /// <summary>
    /// Sets the soft target.
    /// </summary>
    /// <param name="gameObject">GameObject to target.</param>
    public void SetSoftTarget(GameObject? gameObject);
    
    /// <summary>
    /// Sets the current target.
    /// </summary>
    /// <param name="gameObjectAddress">GameObject (address) to target.</param>
    public void SetTarget(nint gameObjectAddress);

    /// <summary>
    /// Sets the mouseover target.
    /// </summary>
    /// <param name="gameObjectAddress">GameObject (address) to target.</param>
    public void SetMouseOverTarget(nint gameObjectAddress);

    /// <summary>
    /// Sets the focus target.
    /// </summary>
    /// <param name="gameObjectAddress">GameObject (address) to target.</param>
    public void SetFocusTarget(nint gameObjectAddress);

    /// <summary>
    /// Sets the previous target.
    /// </summary>
    /// <param name="gameObjectAddress">GameObject (address) to target.</param>
    public void SetPreviousTarget(nint gameObjectAddress);

    /// <summary>
    /// Sets the soft target.
    /// </summary>
    /// <param name="gameObjectAddress">GameObject (address) to target.</param>
    public void SetSoftTarget(nint gameObjectAddress);

    /// <summary>
    /// Clears the current target.
    /// </summary>
    public void ClearTarget();

    /// <summary>
    /// Clears the mouseover target.
    /// </summary>
    public void ClearMouseOverTarget();

    /// <summary>
    /// Clears the focus target.
    /// </summary>
    public void ClearFocusTarget();

    /// <summary>
    /// Clears the previous target.
    /// </summary>
    public void ClearPreviousTarget();

    /// <summary>
    /// Clears the soft target.
    /// </summary>
    public void ClearSoftTarget();
}

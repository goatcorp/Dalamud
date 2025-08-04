using System.Numerics;

using Dalamud.Game.Gui;
using Dalamud.Game.NativeWrapper;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace Dalamud.Plugin.Services;

/// <summary>
/// A class handling many aspects of the in-game UI.
/// </summary>
public unsafe interface IGameGui
{
    /// <summary>
    /// Event which is fired when the game UI hiding is toggled.
    /// </summary>
    public event EventHandler<bool> UiHideToggled;

    /// <summary>
    /// Event that is fired when the currently hovered item changes.
    /// </summary>
    public event EventHandler<ulong> HoveredItemChanged;

    /// <summary>
    /// Event that is fired when the currently hovered action changes.
    /// </summary>
    public event EventHandler<HoveredAction> HoveredActionChanged;

    /// <summary>
    /// Gets a value indicating whether the game UI is hidden.
    /// </summary>
    public bool GameUiHidden { get; }

    /// <summary>
    /// Gets or sets the item ID that is currently hovered by the player. 0 when no item is hovered.
    /// If > 1.000.000, subtract 1.000.000 and treat it as HQ.
    /// </summary>
    public ulong HoveredItem { get; set; }
    
    /// <summary>
    /// Gets the action ID that is current hovered by the player. 0 when no action is hovered.
    /// </summary>
    public HoveredAction HoveredAction { get; }

    /// <summary>
    /// Opens the in-game map with a flag on the location of the parameter.
    /// </summary>
    /// <param name="mapLink">Link to the map to be opened.</param>
    /// <returns>True if there were no errors and it could open the map.</returns>
    public bool OpenMapWithMapLink(MapLinkPayload mapLink);

    /// <summary>
    /// Converts in-world coordinates to screen coordinates (upper left corner origin).
    /// </summary>
    /// <param name="worldPos">Coordinates in the world.</param>
    /// <param name="screenPos">Converted coordinates.</param>
    /// <returns>True if worldPos corresponds to a position in front of the camera and screenPos is in the viewport.</returns>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos);

    /// <summary>
    /// Converts in-world coordinates to screen coordinates (upper left corner origin).
    /// </summary>
    /// <param name="worldPos">Coordinates in the world.</param>
    /// <param name="screenPos">Converted coordinates.</param>
    /// <param name="inView">True if screenPos corresponds to a position inside the camera viewport.</param>
    /// <returns>True if worldPos corresponds to a position in front of the camera.</returns>
    public bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out bool inView);

    /// <summary>
    /// Converts screen coordinates to in-world coordinates via raycasting.
    /// </summary>
    /// <param name="screenPos">Screen coordinates.</param>
    /// <param name="worldPos">Converted coordinates.</param>
    /// <param name="rayDistance">How far to search for a collision.</param>
    /// <returns>True if successful. On false, worldPos's contents are undefined.</returns>
    public bool ScreenToWorld(Vector2 screenPos, out Vector3 worldPos, float rayDistance = 100000.0f);

    /// <summary>
    /// Gets a pointer to the game's UIModule instance.
    /// </summary>
    /// <returns>A pointer wrapper to UIModule.</returns>
    public UIModulePtr GetUIModule();

    /// <summary>
    /// Gets the pointer to the Addon with the given name and index.
    /// </summary>
    /// <param name="name">Name of addon to find.</param>
    /// <param name="index">Index of addon to find (1-indexed).</param>
    /// <returns>A pointer wrapper to the addon.</returns>
    public AtkUnitBasePtr GetAddonByName(string name, int index = 1);

    /// <summary>
    /// Find the agent associated with an addon, if possible.
    /// </summary>
    /// <param name="id">The agent id.</param>
    /// <returns>A pointer wrapper to the agent interface.</returns>
    public AgentInterfacePtr GetAgentById(int id);

    /// <summary>
    /// Find the agent associated with an addon, if possible.
    /// </summary>
    /// <param name="addonName">The addon name.</param>
    /// <returns>A pointer wrapper to the agent interface.</returns>
    public AgentInterfacePtr FindAgentInterface(string addonName);

    /// <summary>
    /// Find the agent associated with an addon, if possible.
    /// </summary>
    /// <param name="addon">The addon address.</param>
    /// <returns>A pointer wrapper to the agent interface.</returns>
    public AgentInterfacePtr FindAgentInterface(AtkUnitBasePtr addon);
}

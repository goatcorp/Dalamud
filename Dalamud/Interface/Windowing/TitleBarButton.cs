using System.Numerics;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Structure describing a title bar button.
/// </summary>
public class TitleBarButton
{
    /// <summary>
    /// Gets or sets the icon of the button.
    /// </summary>
    public FontAwesomeIcon Icon { get; set; }

    /// <summary>
    /// Gets or sets a vector by which the position of the icon within the button shall be offset.
    /// Automatically scaled by the global font scale for you.
    /// </summary>
    public Vector2 IconOffset { get; set; }

    /// <summary>
    /// Gets or sets an action that is called when a tooltip shall be drawn.
    /// May be null if no tooltip shall be drawn.
    /// </summary>
    public Action? ShowTooltip { get; set; }

    /// <summary>
    /// Gets or sets an action that is called when the button is clicked.
    /// </summary>
    public Action<ImGuiMouseButton> Click { get; set; }

    /// <summary>
    /// Gets or sets the priority the button shall be shown in.
    /// Lower = closer to ImGui default buttons.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the button shall be clickable
    /// when the respective window is set to clickthrough.
    /// </summary>
    public bool AvailableClickthrough { get; set; }
}

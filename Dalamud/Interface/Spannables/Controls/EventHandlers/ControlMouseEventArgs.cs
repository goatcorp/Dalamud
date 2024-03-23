using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Mouse event arguments.</summary>
public struct ControlMouseEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public ControlSpannable Sender;

    /// <summary>Location of the mouse in local coordinates.</summary>
    public Vector2 LocalLocation;

    /// <summary>The mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button;

    /// <summary>Number of consequent clicks, for dealing with double clicks.</summary>
    public int Clicks;

    /// <summary>The number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.</summary>
    public Vector2 Delta;
}

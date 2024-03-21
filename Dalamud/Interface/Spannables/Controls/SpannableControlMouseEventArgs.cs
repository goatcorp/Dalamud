using System.Numerics;

using Dalamud.Interface.Spannables.Rendering;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>Mouse event arguments.</summary>
public struct SpannableControlMouseEventArgs
{
    /// <summary>The location of the mouse, relative to the left top of the control, without having
    /// <see cref="RenderState.Transformation"/> applied.</summary>
    public Vector2 Location;

    /// <summary>The mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button;

    /// <summary>Number of consequent clicks, for dealing with double clicks.</summary>
    public int Clicks;

    /// <summary>The number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.</summary>
    public Vector2 Delta;
}

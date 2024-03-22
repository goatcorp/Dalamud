using System.Numerics;

using Dalamud.Interface.Spannables.Controls;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlerArgs;

/// <summary>Mouse event arguments.</summary>
public struct SpannableControlMouseEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public SpannableControl Sender;

    /// <summary>The location of the mouse, relative to the left top of the control, without having
    /// <see cref="ISpannableState.Transformation"/> or <see cref="ISpannableState.ScreenOffset"/> applied.</summary>
    public Vector2 LocalLocation;

    /// <summary>The mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button;

    /// <summary>Number of consequent clicks, for dealing with double clicks.</summary>
    public int Clicks;

    /// <summary>The number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.</summary>
    public Vector2 Delta;
}

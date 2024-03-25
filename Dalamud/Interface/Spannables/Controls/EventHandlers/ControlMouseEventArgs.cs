using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Mouse event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlMouseEventArgs : SpannableControlEventArgs
{
    /// <summary>Gets or sets the location of the mouse in local coordinates.</summary>
    public Vector2 LocalLocation { get; set; }

    /// <summary>Gets or sets the delta of <see cref="LocalLocation"/> since the last event invocation.</summary>
    public Vector2 LocalLocationDelta { get; set; }

    /// <summary>Gets or sets the mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button { get; set; }

    /// <summary>Gets or sets the number of consequent clicks, for dealing with double clicks.</summary>
    public int Clicks { get; set; }

    /// <summary>Gets or sets the number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.
    /// </summary>
    public Vector2 WheelDelta { get; set; }
}

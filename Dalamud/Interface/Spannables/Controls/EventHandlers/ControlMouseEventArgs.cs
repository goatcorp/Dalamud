using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Mouse event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlMouseEventArgs : SpannableControlEventArgs
{
    /// <summary>Gets or sets a value indicating whether the event was handled.</summary>
    /// <value><c>true</c> to bypass the control's default handling; otherwise, <c>false</c> to also pass the event
    /// along to the default control handler.</value>
    public bool Handled { get; set; }

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

    /// <inheritdoc/>
    public override bool TryReset()
    {
        this.LocalLocation = Vector2.Zero;
        this.LocalLocationDelta = Vector2.Zero;
        this.Button = default;
        this.Clicks = 0;
        this.WheelDelta = Vector2.Zero;
        return base.TryReset();
    }
}

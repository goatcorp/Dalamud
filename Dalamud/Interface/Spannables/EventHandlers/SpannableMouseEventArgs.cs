using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.Spannables.Helpers;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Mouse event arguments.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableMouseEventArgs : SpannableEventArgs
{
    private Vector2? localLocationCached;
    private Vector2? localLocationDeltaCached;

    /// <inheritdoc cref="SpannableEventArgs.Sender"/>
    public new Spannable Sender => (Spannable)base.Sender;

    /// <summary>Gets the location of the mouse in screen coordinates.</summary>
    public Vector2 ScreenLocation { get; private set; }

    /// <summary>Gets the change in <see cref="ScreenLocation"/>.</summary>
    public Vector2 ScreenLocationDelta { get; private set; }

    /// <summary>Gets the location of the mouse in local coordinates.</summary>
    public Vector2 LocalLocation => this.localLocationCached ??= this.Sender.PointToClient(this.ScreenLocation);

    /// <summary>Gets the delta of <see cref="LocalLocation"/> since the last event invocation.</summary>
    public Vector2 LocalLocationDelta =>
        this.localLocationDeltaCached ??=
            this.Sender.PointToClient(this.ScreenLocation + this.ScreenLocationDelta) - this.LocalLocation;

    /// <summary>Gets the mouse button that has been pressed or released.</summary>
    public ImGuiMouseButton Button { get; private set; }

    /// <summary>Gets the number of consequent clicks, for dealing with double clicks.</summary>
    public int Clicks { get; private set; }

    /// <summary>Gets the number of immediate repeats, in case an event should be handled multiple times
    /// immediately.</summary>
    public int ImmediateRepeats { get; private set; }

    /// <summary>Gets the number of detents the mouse wheel has rotated, <b>without</b> WHEEL_DELTA multiplied.
    /// </summary>
    public Vector2 WheelDelta { get; private set; }

    /// <summary>Initializes mouse related properties of this instance of <see cref="SpannableMouseEventArgs"/>.
    /// </summary>
    /// <param name="screenLocation">Location of the mouse pointer in screen coordinates.</param>
    /// <param name="screenLocationDelta">Delta of <see cref="ScreenLocation"/>.</param>
    /// <param name="button">The relevant mouse button.</param>
    /// <param name="clicks">Accumulated number of clicks.</param>
    /// <param name="immediateRepeats">Number of immediate repeats.</param>
    /// <param name="wheelDelta">Wheel delta.</param>
    public void InitializeMouseEvent(
        Vector2 screenLocation,
        Vector2 screenLocationDelta,
        ImGuiMouseButton button = (ImGuiMouseButton)(-1),
        int clicks = 0,
        int immediateRepeats = 1,
        Vector2 wheelDelta = default)
    {
        this.ScreenLocation = screenLocation;
        this.ScreenLocationDelta = screenLocationDelta;
        this.localLocationCached = null;
        this.localLocationDeltaCached = null;
        this.Button = button;
        this.Clicks = clicks;
        this.ImmediateRepeats = immediateRepeats;
        this.WheelDelta = wheelDelta;
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Controls.Gestures;

/// <summary>Helper for tracking mouse activity and recognizing gestures.</summary>
public sealed class MouseActivityTracker : IDisposable
{
    private readonly List<Activity> activities = new();

    private bool enabled = true;

    private bool useLeftDrag;
    private bool useRightDrag;
    private bool useMiddleDrag;

    private long clickTimerNext = long.MaxValue;
    private long clickTimerFireLeftClickAfter = long.MaxValue;
    private long clickTimerFireRightClickAfter = long.MaxValue;
    private long clickTimerFireMiddleClickAfter = long.MaxValue;

    /// <summary>Initializes a new instance of the <see cref="MouseActivityTracker"/> class.</summary>
    /// <param name="control">The control to attach itself to.</param>
    public MouseActivityTracker(ControlSpannable control)
    {
        this.Control = control;
        this.Control.MouseDown += this.OnMouseDown;
        this.Control.MouseMove += this.OnMouseMove;
        this.Control.MouseUp += this.OnMouseUp;
        this.Control.MouseWheel += this.OnMouseWheel;
        this.Control.PreDispatchEvents += this.OnPreDispatchEvents;
    }

    /// <summary>Delegate for callbacks on panning.</summary>
    /// <param name="delta">Panned distance.</param>
    public delegate void PanDelegate(Vector2 delta);

    /// <summary>Delegate for callbacks on zooming.</summary>
    /// <param name="origin">The origin of zooming, in local coordinates.</param>
    /// <param name="delta">The zoomed amount.</param>
    public delegate void ZoomDelegate(Vector2 origin, float delta);

    /// <summary>Delegate for callbacks on clicks that may be a part of double clicks.</summary>
    /// <param name="location">The mouse location, in local coordinates.</param>
    public delegate void ClickDelegate(Vector2 location);

    /// <summary>Delegate for callbacks for deciding whether to decide to confirm that it's a double click.</summary>
    /// <param name="location">The mouse location, in local coordinates.</param>
    /// <param name="blockBecomingDoubleClick">Whether to block recognizing the event as a double click.</param>
    public delegate void BarrieredClickDelegate(Vector2 location, ref bool blockBecomingDoubleClick);

    /// <summary>Occurs when dragging has just begun.</summary>
    public event Action? DragStart;

    /// <summary>Occurs when dragging has just ended.</summary>
    public event Action? DragEnd;

    /// <summary>Occurs when the control is panned.</summary>
    public event PanDelegate? Pan;

    /// <summary>Occurs when the control is zoomed by dragging after double clicking.</summary>
    public event ZoomDelegate? DoubleClickDragZoom;

    /// <summary>Occurs when the control is zoomed by scrolling with mouse wheel.</summary>
    public event ZoomDelegate? WheelZoom;

    /// <summary>Occurs when the control is clicked with the left mouse button.</summary>
    public event BarrieredClickDelegate? LeftImmediateClick;

    /// <summary>Occurs when the control is clicked with the right mouse button.</summary>
    public event BarrieredClickDelegate? RightImmediateClick;

    /// <summary>Occurs when the control is clicked with the middle mouse button.</summary>
    public event BarrieredClickDelegate? MiddleImmediateClick;

    /// <summary>Occurs when the control is clicked with the left mouse button, without being a part of a double click.
    /// </summary>
    public event ClickDelegate? LeftClick;

    /// <summary>Occurs when the control is clicked with the right mouse button, without being a part of a double click.
    /// </summary>
    public event ClickDelegate? RightClick;

    /// <summary>Occurs when the control is clicked with the middle mouse button, without being a part of a double
    /// click.</summary>
    public event ClickDelegate? MiddleClick;

    /// <summary>Occurs when the control is double clicked with the left mouse button.</summary>
    public event ClickDelegate? LeftDoubleClick;

    /// <summary>Occurs when the control is double clicked with the right mouse button.</summary>
    public event ClickDelegate? RightDoubleClick;

    /// <summary>Occurs when the control is double clicked with the middle mouse button.</summary>
    public event ClickDelegate? MiddleDoubleClick;

    /// <summary>The recorded activity type.</summary>
    public enum ActivityType
    {
        /// <summary>Mouse button has been pressed down.</summary>
        Down,

        /// <summary>Dragging with mouse has begun.</summary>
        DragStart,

        /// <summary>Mouse button has been released.</summary>
        Up,

        /// <summary>Dragging with mouse has ended.</summary>
        DragEnd,
    }

    /// <summary>Specify how to recognize zooming with wheel scrolls.</summary>
    public enum WheelZoomMode
    {
        /// <summary>Zooming using wheels is disabled.</summary>
        Disabled,

        /// <summary>Always interpret wheel scrolls as zoom.</summary>
        Always,

        /// <summary>Require control key to interpret wheel scrolls as zoom.</summary>
        RequireControlKey,
    }

    /// <summary>Gets or sets a value indicating whether to recognize double clicks using the left mouse button.
    /// </summary>
    public bool UseLeftDouble { get; set; }

    /// <summary>Gets or sets a value indicating whether to recognize double clicks using the right mouse button.
    /// </summary>
    public bool UseRightDouble { get; set; }

    /// <summary>Gets or sets a value indicating whether to recognize double clicks using the middle mouse button.
    /// </summary>
    public bool UseMiddleDouble { get; set; }

    /// <summary>Gets or sets a value indicating whether to use infinite dragging using the left mouse button.</summary>
    public bool UseInfiniteLeftDrag { get; set; }

    /// <summary>Gets or sets a value indicating whether to use infinite dragging using the right mouse button.
    /// </summary>
    public bool UseInfiniteRightDrag { get; set; }

    /// <summary>Gets or sets a value indicating whether to use infinite dragging using the middle mouse button.
    /// </summary>
    public bool UseInfiniteMiddleDrag { get; set; }

    /// <summary>Gets or sets a value indicating whether to use dragging using the left mouse botton.</summary>
    public bool UseLeftDrag
    {
        get => this.useLeftDrag;
        set
        {
            this.useLeftDrag = value;
            if (!value && this.FirstHeldButton == ImGuiMouseButton.Left)
                this.ExitDragState();
        }
    }

    /// <summary>Gets or sets a value indicating whether to use dragging using the right mouse botton.</summary>
    public bool UseRightDrag
    {
        get => this.useRightDrag;
        set
        {
            this.useRightDrag = value;
            if (!value && this.FirstHeldButton == ImGuiMouseButton.Right)
                this.ExitDragState();
        }
    }

    /// <summary>Gets or sets a value indicating whether to use dragging using the middle mouse botton.</summary>
    public bool UseMiddleDrag
    {
        get => this.useMiddleDrag;
        set
        {
            this.useMiddleDrag = value;
            if (!value && this.FirstHeldButton == ImGuiMouseButton.Middle)
                this.ExitDragState();
        }
    }

    /// <summary>Gets or sets the wheel zoom mode.</summary>
    public WheelZoomMode UseWheelZoom { get; set; }

    /// <summary>Gets or sets a value indicating whether to recognize zooming via dragging following a double mouse
    /// button down.</summary>
    public bool UseDoubleClickDragZoom { get; set; }

    /// <summary>Gets or sets a value indicating whether to recognize mouse activities.</summary>
    public bool Enabled
    {
        get => this.enabled;
        set
        {
            this.enabled = value;
            if (!value)
                this.CancelAllOperations();
        }
    }

    /// <summary>Gets the control that this <see cref="MouseActivityTracker"/> is attached to.</summary>
    public ControlSpannable Control { get; }

    /// <summary>Gets the origin of dragging, which is the local coordinates when mouse was held down.</summary>
    public Vector2? DragOrigin { get; private set; }

    /// <summary>Gets the base of dragging, which is the local coordinates that mouse movements are being compared to.
    /// </summary>
    public Vector2? DragBase { get; private set; }

    /// <summary>Gets a value indicating whether the control is being dragged inside.</summary>
    public bool IsDragging => this.DragBase is not null;

    /// <summary>Gets a value indicating whether the control is being dragged inside endlessly.</summary>
    public bool IsInfiniteDragging { get; private set; }

    /// <summary>Gets a value indicating whether the control is being dragged for zooming.</summary>
    public bool IsDraggingZoom { get; private set; }

    /// <summary>Gets a value indicating whether the control is being panned.</summary>
    public bool IsDraggingPan => this.IsDragging && !this.IsDraggingZoom;

    /// <summary>Gets the mouse button that was held down for the first time, since the last time no mouse button was
    /// held down.</summary>
    public ImGuiMouseButton FirstHeldButton { get; private set; } = ImGuiMouseButton.COUNT;

    /// <summary>Gets a value indicating whether the left mouse button is held down, and this instance of
    /// <see cref="MouseActivityTracker"/> recognized it.</summary>
    public bool IsLeftHeld { get; private set; }

    /// <summary>Gets a value indicating whether the right mouse button is held down, and this instance of
    /// <see cref="MouseActivityTracker"/> recognized it.</summary>
    public bool IsRightHeld { get; private set; }

    /// <summary>Gets a value indicating whether the middle mouse button is held down, and this instance of
    /// <see cref="MouseActivityTracker"/> recognized it.</summary>
    public bool IsMiddleHeld { get; private set; }

    /// <summary>Gets a value indicating whether any of the mouse buttons are held down, and this instance of
    /// <see cref="MouseActivityTracker"/> recognized it.</summary>
    public bool IsAnyHeld => this.IsLeftHeld || this.IsRightHeld || this.IsMiddleHeld;

    /// <summary>Gets a value indicating whether the left mouse button has been held down twice in a row.</summary>
    public bool IsLeftDoubleDown { get; private set; }

    /// <summary>Gets a value indicating whether the right mouse button has been held down twice in a row.</summary>
    public bool IsRightDoubleDown { get; private set; }

    /// <summary>Gets a value indicating whether the middle mouse button has been held down twice in a row.</summary>
    public bool IsMiddleDoubleDown { get; private set; }

    /// <summary>Gets a value indicating whether the left mouse button has been released twice in a row.</summary>
    public bool IsLeftDoubleUp { get; private set; }

    /// <summary>Gets a value indicating whether the right mouse button has been released twice in a row.</summary>
    public bool IsRightDoubleUp { get; private set; }

    /// <summary>Gets a value indicating whether the middle mouse button has been released twice in a row.</summary>
    public bool IsMiddleDoubleUp { get; private set; }

    private static Vector2 DoubleClickSize => new(
        GetSystemMetrics(SM.SM_CXDOUBLECLK),
        GetSystemMetrics(SM.SM_CYDOUBLECLK));

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Control.MouseDown -= this.OnMouseDown;
        this.Control.MouseMove -= this.OnMouseMove;
        this.Control.MouseUp -= this.OnMouseUp;
        this.Control.MouseWheel -= this.OnMouseWheel;
        this.Control.PreDispatchEvents -= this.OnPreDispatchEvents;
    }

    /// <summary>Cancels all ongoing operations.</summary>
    public void CancelAllOperations()
    {
        this.ExitDragState();
        this.activities.Clear();
        this.FirstHeldButton = ImGuiMouseButton.COUNT;
        this.IsLeftHeld = this.IsRightHeld = this.IsMiddleHeld = false;
        this.IsLeftDoubleDown = this.IsRightDoubleDown = this.IsMiddleDoubleDown = false;
        this.IsLeftDoubleUp = this.IsRightDoubleUp = this.IsMiddleDoubleUp = false;
        this.FirstHeldButton = ImGuiMouseButton.COUNT;
        this.clickTimerNext = long.MaxValue;
        this.clickTimerFireLeftClickAfter = long.MaxValue;
        this.clickTimerFireRightClickAfter = long.MaxValue;
        this.clickTimerFireMiddleClickAfter = long.MaxValue;
    }

    private void OnMouseDown(SpannableMouseEventArgs e)
    {
        if (!this.enabled || !this.Control.IsMouseHovered
            || e.SuppressHandling || e.Step == SpannableEventStep.BeforeChildren)
            return;

        this.RecordActivity(new(ActivityType.Down, e.Button, e.LocalLocation));

        if (this.FirstHeldButton == ImGuiMouseButton.COUNT)
            this.FirstHeldButton = e.Button;

        var startDrag = false;
        switch (e.Button)
        {
            case ImGuiMouseButton.Left:
                this.IsLeftHeld = true;
                this.IsLeftDoubleDown = this.IsDoubleDownOrUp();
                this.IsDraggingZoom = this.UseDoubleClickDragZoom && this.IsLeftDoubleDown;
                startDrag = this.useLeftDrag;
                this.clickTimerFireLeftClickAfter = long.MaxValue;
                break;

            case ImGuiMouseButton.Right:
                this.IsRightHeld = true;
                this.IsRightDoubleDown = this.IsDoubleDownOrUp();
                this.IsDraggingZoom = this.UseDoubleClickDragZoom && this.IsRightDoubleDown;
                startDrag = this.useRightDrag;
                this.clickTimerFireRightClickAfter = long.MaxValue;
                break;

            case ImGuiMouseButton.Middle:
                this.IsMiddleHeld = true;
                this.IsMiddleDoubleDown = this.IsDoubleDownOrUp();
                this.IsDraggingZoom = this.UseDoubleClickDragZoom && this.IsMiddleDoubleDown;
                startDrag = this.useMiddleDrag;
                this.clickTimerFireMiddleClickAfter = long.MaxValue;
                break;
        }

        this.ProcessClickTimers(Environment.TickCount64);

        if (startDrag && this.DragOrigin is null)
            this.DragOrigin = e.LocalLocation;
    }

    private void OnMouseMove(SpannableMouseEventArgs e)
    {
        if (!this.enabled || !this.Control.IsMouseHovered || e.Step == SpannableEventStep.BeforeChildren)
            return;

        if (this.DragOrigin is not { } dragOrigin)
            return;

        if (e.SuppressHandling)
        {
            this.CancelAllOperations();
            return;
        }

        Vector2 delta;
        if (this.DragBase is { } dragBase)
        {
            var pos = e.LocalLocation;
            delta = new(pos.X - dragBase.X, pos.Y - dragBase.Y);
            if (this.IsInfiniteDragging)
                this.UpdateMousePosImmediately(dragBase);
            else
                this.DragBase = pos;
        }
        else if (
            this.DragOrigin is not null &&
            ((this.useLeftDrag && this.IsLeftHeld) ||
             (this.useRightDrag && this.IsRightHeld) ||
             (this.useMiddleDrag && this.IsMiddleHeld)))
        {
            if ((e.LocalLocation - this.DragOrigin.Value).LengthSquared() <
                WindowsUiConfigHelper.GetMinDragDistance().LengthSquared())
            {
                return;
            }

            this.Control.CaptureMouse = true;
            this.Control.MouseCursor = ImGuiMouseCursor.None;
            var doubleClickRect = RectVector4.Expand(new(dragOrigin), DoubleClickSize / 2f);
            delta = new(e.LocalLocation.X - dragOrigin.X, e.LocalLocation.Y - dragOrigin.Y);
            if ((this.FirstHeldButton == ImGuiMouseButton.Left && !this.UseLeftDouble) ||
                (this.FirstHeldButton == ImGuiMouseButton.Right && !this.UseRightDouble) ||
                (this.FirstHeldButton == ImGuiMouseButton.Middle && !this.UseMiddleDouble) ||
                !doubleClickRect.Contains(e.LocalLocation))
            {
                this.EnterDragState(e.LocalLocation);
            }
        }
        else
        {
            return;
        }

        if (this.IsDragging && delta != default)
        {
            e.SuppressHandling = true;

            var controlAbs = this.Control.PointToScreen(Vector2.Zero);

            if (this.UseDoubleClickDragZoom && this.FirstHeldButton switch
                {
                    ImGuiMouseButton.Left => this.IsLeftDoubleDown,
                    ImGuiMouseButton.Middle => this.IsMiddleDoubleDown,
                    ImGuiMouseButton.Right => this.IsRightDoubleDown,
                    _ => false,
                })
            {
                var dn = delta.Y;
                if (dn != 0)
                    this.DoubleClickDragZoom?.Invoke(dragOrigin, dn);
            }
            else
            {
                this.Pan?.Invoke(delta);
            }

            if (!this.IsInfiniteDragging)
            {
                var controlAbsNew = this.Control.PointToScreen(Vector2.Zero);

                this.DragBase = new(
                    (this.DragBase!.Value.X + controlAbs.X) - controlAbsNew.X,
                    (this.DragBase!.Value.Y + controlAbs.Y) - controlAbsNew.Y);
            }
        }
    }

    private void OnMouseUp(SpannableMouseEventArgs e)
    {
        if (!this.enabled || e.Step == SpannableEventStep.BeforeChildren)
            return;

        if (e.SuppressHandling)
        {
            this.CancelAllOperations();
            return;
        }

        this.RecordActivity(new(ActivityType.Up, e.Button, e.LocalLocation));

        this.IsLeftDoubleUp = this.IsRightDoubleUp = this.IsMiddleDoubleUp = false;
        switch (e.Button)
        {
            case ImGuiMouseButton.Left:
            {
                this.IsLeftHeld = false;
                if (!this.activities[^1].IsInDoubleClickRange(e.LocalLocation))
                    break;

                var eligibleForClick = this.FirstHeldButton == ImGuiMouseButton.Left && !this.IsDragging;
                if (!eligibleForClick)
                {
                    this.activities.Clear();
                    this.IsLeftDoubleUp = false;
                }
                else
                {
                    this.IsLeftDoubleUp = this.IsDoubleDownOrUp();

                    var blockDouble = false;
                    this.LeftImmediateClick?.Invoke(e.LocalLocation, ref blockDouble);
                    if (!this.UseLeftDouble)
                        this.LeftClick?.Invoke(e.LocalLocation);
                    if (blockDouble)
                        this.IsLeftDoubleUp = false;

                    if (this.IsLeftDoubleUp)
                    {
                        this.activities.Clear();
                        this.LeftDoubleClick?.Invoke(e.LocalLocation);
                    }
                    else if (!blockDouble && this.UseLeftDouble && !this.IsDragging)
                    {
                        this.clickTimerFireLeftClickAfter = Environment.TickCount64 + GetDoubleClickTime();
                    }
                }

                break;
            }

            case ImGuiMouseButton.Right:
            {
                this.IsRightHeld = false;
                if (!this.activities[^1].IsInDoubleClickRange(e.LocalLocation))
                    break;

                var eligibleForClick = this.FirstHeldButton == ImGuiMouseButton.Right && !this.IsDragging;
                if (!eligibleForClick)
                {
                    this.activities.Clear();
                    this.IsRightDoubleUp = false;
                }
                else
                {
                    this.IsRightDoubleUp = this.IsDoubleDownOrUp();

                    var blockDouble = false;
                    this.RightImmediateClick?.Invoke(e.LocalLocation, ref blockDouble);
                    if (!this.UseRightDouble)
                        this.RightClick?.Invoke(e.LocalLocation);
                    if (blockDouble)
                        this.IsRightDoubleUp = false;

                    if (this.IsRightDoubleUp)
                    {
                        this.activities.Clear();
                        this.RightDoubleClick?.Invoke(e.LocalLocation);
                    }
                    else if (!blockDouble && this.UseRightDouble && !this.IsDragging)
                    {
                        this.clickTimerFireRightClickAfter = Environment.TickCount64 + GetDoubleClickTime();
                    }
                }

                break;
            }

            case ImGuiMouseButton.Middle:
            {
                this.IsMiddleHeld = false;
                if (!this.activities[^1].IsInDoubleClickRange(e.LocalLocation))
                    break;

                var eligibleForClick = this.FirstHeldButton == ImGuiMouseButton.Middle && !this.IsDragging;
                if (!eligibleForClick)
                {
                    this.activities.Clear();
                    this.IsMiddleDoubleUp = false;
                }
                else
                {
                    this.IsMiddleDoubleUp = this.IsDoubleDownOrUp();

                    var blockDouble = false;
                    this.MiddleImmediateClick?.Invoke(e.LocalLocation, ref blockDouble);
                    if (!this.UseMiddleDouble)
                        this.MiddleClick?.Invoke(e.LocalLocation);
                    if (blockDouble)
                        this.IsMiddleDoubleUp = false;

                    if (this.IsMiddleDoubleUp)
                    {
                        this.activities.Clear();
                        this.MiddleDoubleClick?.Invoke(e.LocalLocation);
                    }
                    else if (!blockDouble && this.UseMiddleDouble && !this.IsDragging)
                    {
                        this.clickTimerFireMiddleClickAfter = Environment.TickCount64 + GetDoubleClickTime();
                    }
                }

                break;
            }
        }

        this.ProcessClickTimers(Environment.TickCount64);

        if (this.FirstHeldButton switch
            {
                ImGuiMouseButton.Left => !this.IsLeftHeld,
                ImGuiMouseButton.Right => !this.IsRightHeld,
                ImGuiMouseButton.Middle => !this.IsMiddleHeld,
                _ => false,
            })
        {
            if (this.DragBase is not null)
                e.SuppressHandling = true;
            this.ExitDragState();
        }

        if (!this.IsAnyHeld)
        {
            this.Control.CaptureMouse = false;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
            this.FirstHeldButton = ImGuiMouseButton.COUNT;
        }
    }

    private void OnMouseWheel(SpannableMouseEventArgs e)
    {
        if (!this.enabled || !this.Control.IsMouseHovered
            || e.Step == SpannableEventStep.BeforeChildren || e.SuppressHandling)
            return;

        if (e.WheelDelta == Vector2.Zero)
            return;
        if (
            this.UseWheelZoom is WheelZoomMode.Always ||
            (this.UseWheelZoom is WheelZoomMode.RequireControlKey && ImGui.GetIO().KeyCtrl))
        {
            this.WheelZoom?.Invoke(e.LocalLocation, e.WheelDelta.Y);
            e.SuppressHandling = true;
        }
    }

    private void OnPreDispatchEvents(SpannableEventArgs args)
    {
        if (this.IsDragging)
            ImGui.SetMouseCursor(ImGuiMouseCursor.None);

        var now = Environment.TickCount64;
        if (now < this.clickTimerNext)
            return;

        this.clickTimerNext = long.MaxValue;
        this.ProcessClickTimers(now);
    }

    private void EnterDragState(Vector2 dragBase)
    {
        if (this.DragBase is not null)
            return;

        this.DragBase = dragBase;
        this.RecordActivity(new(ActivityType.DragStart, ImGuiMouseButton.COUNT, this.DragBase.Value));

        if ((this.UseInfiniteLeftDrag && this.FirstHeldButton == ImGuiMouseButton.Left) ||
            (this.UseInfiniteRightDrag && this.FirstHeldButton == ImGuiMouseButton.Right) ||
            (this.UseInfiniteMiddleDrag && this.FirstHeldButton == ImGuiMouseButton.Middle))
        {
            this.IsInfiniteDragging = true;
            this.DragOrigin = dragBase;
            this.DragBase =
                this.Control.PointToClient(
                    ImGui.GetPlatformIO().Monitors[0].MainPos
                    + (ImGui.GetPlatformIO().Monitors[0].MainSize / 2));
            this.UpdateMousePosImmediately(this.DragBase.Value);
        }

        this.DragStart?.Invoke();
    }

    private void ExitDragState()
    {
        if (this.DragOrigin is null)
            return;

        if (this.DragBase is { } dragBase)
        {
            this.Control.MouseCursor = ImGuiMouseCursor.Arrow;
            this.RecordActivity(new(ActivityType.DragEnd, ImGuiMouseButton.COUNT, dragBase));
            if (this.IsInfiniteDragging)
            {
                this.UpdateMousePosImmediately(this.DragOrigin.Value);
                this.IsInfiniteDragging = false;
            }
        }

        this.IsDraggingZoom = false;

        this.DragOrigin = this.DragBase = null;

        this.DragEnd?.Invoke();
    }

    private void RecordActivity(Activity activity)
    {
        if (this.activities.Count >= 8)
            this.activities.RemoveRange(0, (this.activities.Count - 8) + 1);
        this.activities.Add(activity);
    }

    private bool IsDoubleDownOrUp() =>
        this.activities.Count >= 3 &&
        ((this.activities[^1].Button == ImGuiMouseButton.Left && this.UseLeftDouble) ||
         (this.activities[^1].Button == ImGuiMouseButton.Right && this.UseRightDouble) ||
         (this.activities[^1].Button == ImGuiMouseButton.Middle && this.UseMiddleDouble)) &&
        this.activities[^1].Button == this.activities[^3].Button &&
        this.activities[^1].Button == this.activities[^2].Button &&
        this.activities[^1].Type == this.activities[^3].Type &&
        this.activities[^2].Type is not ActivityType.DragEnd and not ActivityType.DragStart &&
        this.activities[^1].Tick - this.activities[^3].Tick <= GetDoubleClickTime() &&
        this.activities[^1].IsInDoubleClickRange(this.activities[^3].Point);

    private void ProcessClickTimers(long now)
    {
        if (this.clickTimerFireLeftClickAfter <= now)
        {
            this.LeftClick?.Invoke(
                this.activities
                    .Select(x => (Activity?)x)
                    .LastOrDefault(x => x!.Value.Button == ImGuiMouseButton.Left && x.Value.Type == ActivityType.Up)
                    ?.Point
                ?? this.Control.PointToClient(ImGui.GetIO().MousePos));
            this.clickTimerFireLeftClickAfter = long.MaxValue;
        }

        if (this.clickTimerFireRightClickAfter <= now)
        {
            this.RightClick?.Invoke(
                this.activities
                    .Select(x => (Activity?)x)
                    .LastOrDefault(x => x!.Value.Button == ImGuiMouseButton.Right && x.Value.Type == ActivityType.Up)
                    ?.Point
                ?? this.Control.PointToClient(ImGui.GetIO().MousePos));
            this.clickTimerFireRightClickAfter = long.MaxValue;
        }

        if (this.clickTimerFireMiddleClickAfter <= now)
        {
            this.MiddleClick?.Invoke(
                this.activities
                    .Select(x => (Activity?)x)
                    .LastOrDefault(x => x!.Value.Button == ImGuiMouseButton.Middle && x.Value.Type == ActivityType.Up)
                    ?.Point
                ?? this.Control.PointToClient(ImGui.GetIO().MousePos));
            this.clickTimerFireMiddleClickAfter = long.MaxValue;
        }

        var next = this.clickTimerFireLeftClickAfter;
        next = Math.Min(next, this.clickTimerFireRightClickAfter);
        next = Math.Min(next, this.clickTimerFireMiddleClickAfter);
        if (next != long.MaxValue)
            this.clickTimerNext = next;
    }

    private unsafe void UpdateMousePosImmediately(Vector2 localCoordinates)
    {
        var c = this.Control.MouseCursor switch
        {
            ImGuiMouseCursor.Arrow => IDC.IDC_ARROW,
            ImGuiMouseCursor.TextInput => IDC.IDC_IBEAM,
            ImGuiMouseCursor.ResizeAll => IDC.IDC_SIZEALL,
            ImGuiMouseCursor.ResizeEW => IDC.IDC_SIZEWE,
            ImGuiMouseCursor.ResizeNS => IDC.IDC_SIZENS,
            ImGuiMouseCursor.ResizeNESW => IDC.IDC_SIZENESW,
            ImGuiMouseCursor.ResizeNWSE => IDC.IDC_SIZENWSE,
            ImGuiMouseCursor.Hand => IDC.IDC_HAND,
            ImGuiMouseCursor.NotAllowed => IDC.IDC_NO,
            _ => null,
        };

        SetCursor(c is null ? default : LoadCursorW(default, c));

        var pos = this.Control.PointToScreen(localCoordinates);
        ImGui.GetIO().MousePos = pos;
        SetCursorPos((int)pos.X, (int)pos.Y);
    }

    private readonly struct Activity
    {
        public readonly long Tick = Environment.TickCount64;
        public readonly ActivityType Type;
        public readonly ImGuiMouseButton Button;
        public readonly Vector2 Point;

        public Activity(ActivityType type, ImGuiMouseButton button, Vector2 point)
        {
            this.Type = type;
            this.Button = button;
            this.Point = point;
        }

        public bool IsInDoubleClickRange(Vector2 point) =>
            RectVector4.Expand(new(point), DoubleClickSize / 2f).Contains(this.Point);
    }
}

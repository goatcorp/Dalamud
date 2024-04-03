using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Controls;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;

using ImGuiNET;

namespace Dalamud.Interface.Spannables;

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
public abstract partial class Spannable
{
    private readonly long[] clickTrackingLastClickTick = new long[(int)ImGuiMouseButton.COUNT];
    private readonly int[] clickTrackingCumulativeCount = new int[(int)ImGuiMouseButton.COUNT];
    private readonly long[] clickTrackingIsHeldDown = new long[(int)ImGuiMouseButton.COUNT];
    private readonly long[] mousePressNextTick = new long[(int)ImGuiMouseButton.COUNT];
    private readonly int[] mousePressCumulativeCount = new int[(int)ImGuiMouseButton.COUNT];
    private int mouseCapturedButtonFlags;

    private ImGuiMouseCursor mouseCursor = ImGuiMouseCursor.Arrow;
    private bool captureMouseOnMouseDown;
    private bool captureMouse;

    /// <summary>Occurs when the mouse pointer is moved over the control.</summary>
    public event SpannableMouseEventHandler? MouseMove;

    /// <summary>Occurs when the mouse pointer enters the control.</summary>
    public event SpannableMouseEventHandler? MouseEnter;

    /// <summary>Occurs when the mouse pointer leaves the control.</summary>
    public event SpannableMouseEventHandler? MouseLeave;

    /// <summary>Occurs when the mouse wheel moves while the control is hovered.</summary>
    public event SpannableMouseEventHandler? MouseWheel;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is pressed.</summary>
    public event SpannableMouseEventHandler? MouseDown;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is released.</summary>
    public event SpannableMouseEventHandler? MouseUp;

    /// <summary>Occurs when the control is clicked by the mouse.</summary>
    public event SpannableMouseEventHandler? MouseClick;

    /// <summary>Occurs when the control has bene held down for a duration and could be given an handling like
    /// key press repeats.</summary>
    public event SpannableMouseEventHandler? MousePress;

    /// <summary>Occurs when the property <see cref="MouseCursor"/> is changing.</summary>
    public event PropertyChangeEventHandler<ImGuiMouseCursor>? MouseCursorChange;

    /// <summary>Occurs when the property <see cref="CaptureMouseOnMouseDown"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CaptureMouseOnMouseDownChange;

    /// <summary>Occurs when the property <see cref="CaptureMouse"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CaptureMouseChange;

    /// <summary>Gets or sets the mouse cursor.</summary>
    public ImGuiMouseCursor MouseCursor
    {
        get => this.mouseCursor;
        set => this.HandlePropertyChange(
            nameof(this.MouseCursor),
            ref this.mouseCursor,
            value,
            this.mouseCursor == value,
            this.OnMouseCursorChange);
    }

    /// <summary>Gets or sets a value indicating whether to capture mouse events when a mouse button is held on
    /// the control.</summary>
    /// <remarks>Enabling this when <see cref="Enabled"/> is set will typically prevent moving the container window by
    /// dragging on what <i>looks</i> like the window background.</remarks>
    public bool CaptureMouseOnMouseDown
    {
        get => this.captureMouseOnMouseDown;
        set => this.HandlePropertyChange(
            nameof(this.CaptureMouseOnMouseDown),
            ref this.captureMouseOnMouseDown,
            value,
            this.captureMouseOnMouseDown == value,
            this.OnCaptureMouseOnMouseDownChange);
    }

    /// <summary>Gets or sets a value indicating whether to capture mouse events, regardless of whether the control is
    /// held.</summary>
    public bool CaptureMouse
    {
        get => this.captureMouse;
        set => this.HandlePropertyChange(
            nameof(this.CaptureMouse),
            ref this.captureMouse,
            value,
            this.captureMouse == value,
            this.OnCaptureMouseChange);
    }

    /// <summary>Gets a value indicating whether the mouse pointer is hovering on this control.</summary>
    public bool IsMouseHovered { get; private set; }

    /// <summary>Gets a value indicating whether the mouse pointer is hovering on this control or any of its child
    /// controls.</summary>
    public bool IsMouseHoveredIncludingChildren
    {
        get
        {
            foreach (var x in this.EnumerateHierarchy<ControlSpannable>())
            {
                if (x.IsMouseHovered)
                    return true;
            }

            return false;
        }
    }

    /// <summary>Gets the tick from <see cref="Environment.TickCount64"/> when the left mouse button is held down.
    /// </summary>
    /// <value><c>0</c> if not currently pressed.</value>
    public long LeftMouseButtonHeldTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.clickTrackingIsHeldDown[0];
    }

    /// <summary>Gets the tick from <see cref="Environment.TickCount64"/> when the right mouse button is held down.
    /// </summary>
    /// <value><c>0</c> if not currently pressed.</value>
    public long RightMouseButtonHeldTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.clickTrackingIsHeldDown[1];
    }

    /// <summary>Gets the tick from <see cref="Environment.TickCount64"/> when the middle mouse button is held down.
    /// </summary>
    /// <value><c>0</c> if not currently pressed.</value>
    public long MiddleMouseButtonHeldTime
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.clickTrackingIsHeldDown[2];
    }

    /// <summary>Gets a value indicating whether the left mouse button is being held down.</summary>
    public bool IsLeftMouseButtonDown
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.LeftMouseButtonHeldTime != 0;
    }

    /// <summary>Gets a value indicating whether the right mouse button is being held down.</summary>
    public bool IsRightMouseButtonDown
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.RightMouseButtonHeldTime != 0;
    }

    /// <summary>Gets a value indicating whether the middle mouse button is being held down.</summary>
    public bool IsMiddleMouseButtonDown
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this.MiddleMouseButtonHeldTime != 0;
    }

    /// <summary>Gets a value indicating whether any mouse button is being held down.</summary>
    public bool IsAnyMouseButtonDown =>
        this.IsLeftMouseButtonDown || this.IsRightMouseButtonDown || this.IsMiddleMouseButtonDown;

    /// <summary>Gets a value indicating whether the mouse pointer is hovering inside <see cref="Boundary"/>.</summary>
    protected bool IsMouseHoveredInsideBoundary { get; private set; }

    private bool ShouldCapture =>
        this.ImGuiGlobalId != 0
        && (this.captureMouse || (this.captureMouseOnMouseDown && this.mouseCapturedButtonFlags != 0))
        && this.enabled && this.visible;

    /// <summary>Raises the <see cref="MouseMove"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseMove(SpannableMouseEventArgs args) => this.MouseMove?.Invoke(args);

    /// <summary>Raises the <see cref="MouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseEnter(SpannableMouseEventArgs args) => this.MouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="MouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseLeave(SpannableMouseEventArgs args) => this.MouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="MouseWheel"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseWheel(SpannableMouseEventArgs args) => this.MouseWheel?.Invoke(args);

    /// <summary>Raises the <see cref="MouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseClick(SpannableMouseEventArgs args) => this.MouseClick?.Invoke(args);

    /// <summary>Raises the <see cref="MousePress"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMousePress(SpannableMouseEventArgs args) => this.MousePress?.Invoke(args);

    /// <summary>Raises the <see cref="MouseDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseDown(SpannableMouseEventArgs args) => this.MouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="MouseUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseUp(SpannableMouseEventArgs args) => this.MouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="MouseCursorChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMouseCursorChange(PropertyChangeEventArgs<ImGuiMouseCursor> args) =>
        this.MouseCursorChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseOnMouseDown"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseOnMouseDownChange(PropertyChangeEventArgs<bool> args) =>
        this.CaptureMouseOnMouseDownChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseChange(PropertyChangeEventArgs<bool> args) =>
        this.CaptureMouseChange?.Invoke(args);

    private bool DispatchMouseMove(Vector2 screenLocation, Vector2 screenLocationDelta, bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var mouseHoveredInsideBoundary =
            this.HitTest(this.PointToClient(screenLocation))
            && (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) || this.ShouldCapture);

        var shouldCapture = this.ShouldCapture;
        var mouseHovered = this.ImGuiIsHoverable && (shouldCapture || mouseHoveredInsideBoundary);

        var dispatchMouseMove =
            mouseHovered && screenLocationDelta != Vector2.Zero && this.InputEventDispatchShouldDispatchToSelf;

        SpannableMouseEventArgs? args = null;

        var thisIsHandling = false;
        if (dispatchMouseMove)
        {
            args = SpannableEventArgsPool.Rent<SpannableMouseEventArgs>();
            args.InitializeMouseEvent(screenLocation, screenLocationDelta);

            args.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            this.OnMouseMove(args);
            thisIsHandling = args.SuppressHandling && !alreadyHandled;
            alreadyHandled |= args.SuppressHandling;
        }

        if (this.InputEventDispatchShouldDispatchToChildren)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
            {
                alreadyHandled |=
                    children[i]?.DispatchMouseMove(screenLocation, screenLocationDelta, alreadyHandled) is true;
            }
        }

        if (dispatchMouseMove)
        {
            args.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            this.OnMouseMove(args);
            thisIsHandling |= args.SuppressHandling && !alreadyHandled;
            alreadyHandled |= args.SuppressHandling;
        }

        if (alreadyHandled && !thisIsHandling)
            shouldCapture = mouseHovered = false;
        else if (mouseHovered && shouldCapture)
            alreadyHandled = true;

        this.IsMouseHoveredInsideBoundary = mouseHoveredInsideBoundary;
        if (this.IsMouseHovered != mouseHovered)
        {
            this.IsMouseHovered = mouseHovered;
            args = SpannableEventArgsPool.Rent<SpannableMouseEventArgs>();
            args.InitializeMouseEvent(screenLocation, screenLocationDelta);
            args.Initialize(this, SpannableEventStep.DirectTarget);
            if (mouseHovered)
                this.OnMouseEnter(args);
            else
                this.OnMouseLeave(args);
        }

        mouseHovered &= this.ImGuiIsHoverable;
        if (mouseHovered)
        {
            SpannableImGuiItem.SetHovered(this, this.selfInnerId, true);
            if (!shouldCapture)
                ImGuiInternals.ImGuiContext.Instance.HoveredIdAllowOverlap = 1;
        }

        this.UpdateMouseCapture(shouldCapture);

        SpannableEventArgsPool.Return(args);
        return alreadyHandled;
    }

    private bool DispatchMouseWheel(
        Vector2 screenLocation,
        Vector2 screenLocationDelta,
        Vector2 delta,
        bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var dispatchMouseWheel =
            (this.ImGuiIsHovered || this.IsMouseHovered) && this.InputEventDispatchShouldDispatchToSelf;

        SpannableMouseEventArgs? args = null;

        if (dispatchMouseWheel)
        {
            args = SpannableEventArgsPool.Rent<SpannableMouseEventArgs>();
            args.InitializeMouseEvent(screenLocation, screenLocationDelta, wheelDelta: delta);

            args.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            this.OnMouseWheel(args);
            alreadyHandled |= args.SuppressHandling;
        }

        if (this.InputEventDispatchShouldDispatchToChildren)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
            {
                alreadyHandled |= children[i]?.DispatchMouseWheel(
                                      screenLocation,
                                      screenLocationDelta,
                                      delta,
                                      alreadyHandled) is true;
            }
        }

        if (dispatchMouseWheel)
        {
            args.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            this.OnMouseWheel(args);
            alreadyHandled |= args.SuppressHandling;
        }

        SpannableEventArgsPool.Return(args);
        return alreadyHandled;
    }

    private bool DispatchMouseDown(
        Vector2 screenLocation,
        Vector2 screenLocationDelta,
        ImGuiMouseButton button,
        bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var dispatchMouseDown = 
            (this.ImGuiIsHovered || this.IsMouseHovered) && this.InputEventDispatchShouldDispatchToSelf;

        this.clickTrackingIsHeldDown[(int)button] = Environment.TickCount64;

        SpannableMouseEventArgs? args = null;
        var thisIsHandling = false;
        if (dispatchMouseDown)
        {
            args = SpannableEventArgsPool.Rent<SpannableMouseEventArgs>();
            args.InitializeMouseEvent(screenLocation, screenLocationDelta, button: button);

            args.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            this.OnMouseDown(args);
            thisIsHandling = args.SuppressHandling && !alreadyHandled;
            alreadyHandled |= args.SuppressHandling;
        }

        if (this.InputEventDispatchShouldDispatchToChildren)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
            {
                alreadyHandled |= children[i]?.DispatchMouseDown(
                                      screenLocation,
                                      screenLocationDelta,
                                      button,
                                      alreadyHandled) is true;
            }
        }

        if (dispatchMouseDown)
        {
            args.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            this.OnMouseDown(args);
            thisIsHandling = args.SuppressHandling && !alreadyHandled;
            alreadyHandled |= args.SuppressHandling;
        }

        var shouldCapture = false;
        thisIsHandling |= !alreadyHandled && this.captureMouseOnMouseDown && dispatchMouseDown;
        if (thisIsHandling)
        {
            // Start tracking clicks and presses, if nobody marked MouseDown as handled so far.

            this.mousePressNextTick[(int)button] =
                Environment.TickCount64 + WindowsUiConfigHelper.GetKeyboardRepeatInitialDelay();

            var prev = this.mouseCapturedButtonFlags;
            this.mouseCapturedButtonFlags |= 1 << (int)button;
            if (prev == 0 && this.captureMouseOnMouseDown)
            {
                alreadyHandled = true;
                shouldCapture = true;
            }
        }
        else
        {
            this.clickTrackingIsHeldDown[(int)button] = 0;
        }

        shouldCapture |= this.captureMouse;
        shouldCapture &= this.visible && this.enabled;

        this.UpdateMouseCapture(shouldCapture);

        SpannableEventArgsPool.Return(args);
        return alreadyHandled;
    }

    private bool DispatchMouseUp(
        Vector2 screenLocation,
        Vector2 screenLocationDelta,
        ImGuiMouseButton button,
        bool alreadyHandled)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return true;

        var dispatchMouseUp = 
            (this.ImGuiIsHovered || this.IsMouseHovered) && this.InputEventDispatchShouldDispatchToSelf;

        this.mouseCapturedButtonFlags &= ~(1 << (int)button);
        this.mousePressNextTick[(int)button] = 0;
        this.mousePressCumulativeCount[(int)button] = 0;

        var clickTrackingIsHeldDownPrev = this.clickTrackingIsHeldDown[(int)button];
        this.clickTrackingIsHeldDown[(int)button] = 0;

        var shouldCapture =
            (this.mouseCapturedButtonFlags != 0 || this.captureMouse) && this.visible && this.enabled;

        SpannableMouseEventArgs? args = null;
        if (dispatchMouseUp)
        {
            args = SpannableEventArgsPool.Rent<SpannableMouseEventArgs>();
            args.InitializeMouseEvent(screenLocation, screenLocationDelta, button: button);

            args.Initialize(this, SpannableEventStep.BeforeChildren, alreadyHandled);
            this.OnMouseUp(args);
            alreadyHandled |= args.SuppressHandling;
        }

        if (this.InputEventDispatchShouldDispatchToChildren)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
            {
                alreadyHandled |=
                    children[i]?.DispatchMouseUp(screenLocation, screenLocationDelta, button, alreadyHandled) is true;
            }
        }

        var shouldClearClickTracking = true;
        if (dispatchMouseUp)
        {
            args.Initialize(this, SpannableEventStep.AfterChildren, alreadyHandled);
            this.OnMouseUp(args);
            alreadyHandled |= args.SuppressHandling;

            if (!alreadyHandled && clickTrackingIsHeldDownPrev != 0 && this.IsMouseHoveredInsideBoundary)
            {
                // Dispatch click to self, if nobody marked MouseUp as handled so far.

                if (this.clickTrackingLastClickTick[(int)button] < Environment.TickCount64)
                    this.clickTrackingCumulativeCount[(int)button] = 1;
                else
                    this.clickTrackingCumulativeCount[(int)button] += 1;

                this.clickTrackingLastClickTick[(int)button] =
                    Environment.TickCount64 + WindowsUiConfigHelper.GetDoubleClickInterval();

                args.InitializeMouseEvent(
                    screenLocation,
                    screenLocationDelta,
                    clicks: this.clickTrackingCumulativeCount[(int)button]);
                args.Initialize(this, SpannableEventStep.DirectTarget);
                this.OnMouseClick(args);

                if (!args.SuppressHandling)
                    shouldClearClickTracking = false;

                alreadyHandled = true;
            }
        }

        this.UpdateMouseCapture(shouldCapture);

        if (shouldClearClickTracking)
        {
            this.clickTrackingCumulativeCount[(int)button] = 0;
            this.clickTrackingLastClickTick[(int)button] = 0;
        }

        SpannableEventArgsPool.Return(args);
        return alreadyHandled;
    }

    private void DispatchMiscMouseEvents(Vector2 screenLocation, Vector2 screenLocationDelta)
    {
        if (this.InputEventDispatchShouldSuppressAll)
            return;

        if (this.InputEventDispatchShouldDispatchToSelf)
        {
            var tc64 = Environment.TickCount64;
            var repeatInterval = WindowsUiConfigHelper.GetKeyboardRepeatInterval();

            SpannableMouseEventArgs? margs = null;
            for (var i = 0; i < (int)ImGuiMouseButton.COUNT; i++)
            {
                if (this.clickTrackingIsHeldDown[i] == 0)
                    continue;

                ref var nextRepeatTime = ref this.mousePressNextTick[i];
                ref var cumulativePressCount = ref this.mousePressCumulativeCount[i];

                if (nextRepeatTime == long.MaxValue)
                    continue; // suppressed

                var d = tc64 - nextRepeatTime;
                if (d < 0)
                    continue; // not yet

                var repeatCount = 1 + (int)(d / repeatInterval);
                cumulativePressCount += repeatCount;
                nextRepeatTime += repeatInterval * repeatCount;

                margs ??= SpannableEventArgsPool.Rent<SpannableMouseEventArgs>();
                margs.InitializeMouseEvent(
                    screenLocation,
                    screenLocationDelta,
                    button: (ImGuiMouseButton)i,
                    clicks: cumulativePressCount,
                    immediateRepeats: repeatCount);
                margs.Initialize(this, SpannableEventStep.BeforeChildren);
                this.OnMousePress(margs);

                if (margs.SuppressHandling)
                    nextRepeatTime = long.MaxValue; // suppress
            }

            SpannableEventArgsPool.Return(margs);
        }

        if (this.InputEventDispatchShouldDispatchToChildren)
        {
            var children = this.GetAllChildSpannables();
            for (var i = children.Count - 1; i >= 0; i--)
                children[i]?.DispatchMiscMouseEvents(screenLocation, screenLocationDelta);
        }

        if (this.IsMouseHovered && this.InputEventDispatchShouldDispatchToSelf)
            ImGui.SetMouseCursor(this.mouseCursor);
    }
}

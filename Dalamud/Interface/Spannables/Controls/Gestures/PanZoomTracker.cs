using System.Numerics;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Controls.Gestures.RectScaleMode;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Numerics;

using ImGuiNET;

using Quaternion = FFXIVClientStructs.FFXIV.Common.Math.Quaternion;

namespace Dalamud.Interface.Spannables.Controls.Gestures;

/// <summary>Translates mouse activities as panning, zooming, and rotation.</summary>
public sealed class PanZoomTracker : IDisposable
{
    private float zoomExponentDividendRange;

    private Vector2 pan;
    private Vector2 size;
    private BorderVector4 panExtraRange;
    private IRectScaleMode defaultScaleMode;
    private IRectScaleMode? scaleMode;
    private float lastKnownZoom;
    private float rotation;

    /// <summary>Initializes a new instance of the <see cref="PanZoomTracker"/> class.</summary>
    /// <param name="mouseActivityTracker">An instance of <see cref="MouseActivityTracker"/> to bind to.</param>
    /// <param name="defaultScaleMode">The initial and default scale mode.</param>
    public PanZoomTracker(MouseActivityTracker mouseActivityTracker, IRectScaleMode? defaultScaleMode = null)
    {
        this.defaultScaleMode = defaultScaleMode ?? FitInClientRectScaleMode.GetInstance(false);
        this.MouseActivity = mouseActivityTracker;
        this.MouseActivity.Pan += this.MouseActivityTrackerOnPan;
        this.MouseActivity.DoubleClickDragZoom += this.MouseActivityTrackerOnDoubleClickDragZoom;
        this.MouseActivity.WheelZoom += this.MouseActivityTrackerOnWheelZoom;
        this.MouseActivity.LeftDoubleClick += this.MouseActivityOnLeftDoubleClick;
        this.MouseActivity.DragEnd += this.MouseActivityOnDragEnd;
        this.Control.MeasuredBoundaryBoxChange += this.ControlOnMeasuredBoundaryBoxChange;

        this.ZoomExponentDivisor = 8;
        this.ZoomExponentDividendRange = 32; // Means that max zoom is 2 ^ (32 / 8).
        this.ZoomExponentDividendWheelDelta = 1 / 16f;
        this.ZoomExponentDividendDragDelta = 1 / 16f;
    }

    /// <summary>Occurs when the viewport changes, because panning, zooming, or rotating has happened.</summary>
    public event Action? ViewportChanged;

    /// <summary>Gets or sets the zoom exponent divisor.</summary>
    public float ZoomExponentDivisor { get; set; }

    /// <summary>Gets or sets the amount of zooming when zooming with mouse wheel.</summary>
    public float ZoomExponentDividendWheelDelta { get; set; }

    /// <summary>Gets or sets the amount of zooming when zooming via drag.</summary>
    public float ZoomExponentDividendDragDelta { get; set; }

    /// <summary>Gets or sets the range of the zoom exponent.</summary>
    public float ZoomExponentDividendRange
    {
        get => this.zoomExponentDividendRange;
        set
        {
            this.zoomExponentDividendRange = value;
            this.EnforceLimits();
        }
    }

    /// <summary>Gets or sets the default scale mode.</summary>
    public IRectScaleMode DefaultScaleMode
    {
        get => this.defaultScaleMode;
        set
        {
            if (this.defaultScaleMode == value)
                return;

            if (this.scaleMode is null)
            {
                this.UpdateScaleMode(value);
                this.scaleMode = null;
                this.defaultScaleMode = value;
            }
            else
            {
                this.defaultScaleMode = value;
                this.EnforceLimits();
            }
        }
    }

    /// <summary>Gets or sets a scale mode that overrides <see cref="DefaultScaleMode"/>.</summary>
    public IRectScaleMode? ScaleMode
    {
        get => this.scaleMode;
        set
        {
            if (this.scaleMode == value)
                return;

            this.UpdateScaleMode(value);
        }
    }

    /// <summary>Gets or sets the pan speed multiplier, which is multiplied by the delta distance of mouse movements.
    /// </summary>
    public float PanSpeedMultiplier { get; set; } = 1f;

    /// <summary>Gets or sets the extra pannable amount at each borders.</summary>
    public BorderVector4 PanExtraRange
    {
        get => this.panExtraRange;
        set
        {
            if (this.panExtraRange == value)
                return;
            this.panExtraRange = value;
            this.EnforceLimits();
        }
    }

    /// <summary>Gets or sets the panned amount. Rotation is not applied to this value.</summary>
    public Vector2 Pan
    {
        get => this.pan;
        set => this.UpdatePan(value);
    }

    /// <summary>Gets or sets the size of the content region.</summary>
    public Vector2 Size
    {
        get => this.size;
        set
        {
            this.size = value;
            this.EnforceLimits();
        }
    }

    /// <summary>Gets or sets the clockwise rotation, in radians.</summary>
    public float Rotation
    {
        get => this.rotation;
        set => this.UpdateRotation(value);
    }

    /// <summary>Gets the underlying instance of <see cref="MouseActivityTracker"/>.</summary>
    public MouseActivityTracker MouseActivity { get; }

    /// <summary>Gets the control this <see cref="PanZoomTracker"/> is bound to.</summary>
    public ControlSpannable Control => this.MouseActivity.Control;

    /// <summary>Gets the curent size of the control.</summary>
    public Vector2 ControlBodySize => this.Control.MeasuredBoundaryBox.Size;

    /// <summary>Gets the effective scale mode.</summary>
    public IRectScaleMode EffectiveScaleMode => this.scaleMode ?? this.defaultScaleMode;

    /// <summary>Gets the effective rotated size, which is <see cref="RotatedSize"/> with <see cref="EffectiveZoom"/>
    /// applied.</summary>
    public Vector2 EffectiveRotatedSize => this.EffectiveZoom * this.RotatedSize;

    /// <summary>Gets the rotated size, which is the maximum horizontal and vertical distances between all four points
    /// of the content region rectangle before scaling.</summary>
    public Vector2 RotatedSize
    {
        get
        {
            var rv = new RectVector4(Vector2.Zero, this.size);
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, this.rotation);
            var lt = Vector2.Transform(rv.LeftTop, q);
            var rt = Vector2.Transform(rv.RightTop, q);
            var rb = Vector2.Transform(rv.RightBottom, q);
            var lb = Vector2.Transform(rv.LeftBottom, q);
            var minPos = Vector2.Min(Vector2.Min(lt, rt), Vector2.Min(lb, rb));
            var maxPos = Vector2.Max(Vector2.Max(lt, rt), Vector2.Max(lb, rb));
            return maxPos - minPos;
        }
    }

    /// <summary>Gets the effective content region size.</summary>
    public Vector2 EffectiveSize => this.EffectiveScaleMode.CalcSize(
        this.Size,
        this.ControlBodySize,
        this.ZoomExponentDivisor);

    /// <summary>Gets the effective zoom scale.</summary>
    public float EffectiveZoom => this.EffectiveScaleMode.CalcZoom(
        this.Size,
        this.ControlBodySize,
        this.ZoomExponentDivisor);

    /// <summary>Gets the effective zoom exponent.</summary>
    public float EffectiveZoomExponent => this.EffectiveScaleMode.CalcZoomExponent(
        this.Size,
        this.ControlBodySize,
        this.ZoomExponentDivisor);

    /// <summary>Gets the default origin, which is the center of the control.</summary>
    public Vector2 DefaultRelativeOrigin
    {
        get
        {
            var box = this.Control.MeasuredInteractiveBox;
            var p = this.Control.PointToClient(ImGui.GetIO().MousePos);
            return this.LocalToContentRelative(box.Contains(p) ? p : box.Center);
        }
    }

    /// <summary>Gets the effect rect of the content region that includes all four corners.</summary>
    public RectVector4 EffectiveRect
    {
        get
        {
            var box = this.Control.MeasuredInteractiveBox;
            var s = this.EffectiveRotatedSize;
            var p = (box.Center - (s / 2)) + this.pan;
            return RectVector4.FromCoordAndSize(p, s);
        }
    }

    /// <summary>Gets a value indicating whether this <see cref="PanZoomTracker"/> is currently enforcing constraints.
    /// </summary>
    /// <remarks>Some operations will temporarily turn disable constraints to reduce jank.</remarks>
    public bool IsPanClampingActive => !this.MouseActivity.IsRightHeld;

    /// <summary>Gets a value indicating whether panning can be done under the current constraints.</summary>
    public bool CanPan =>
        this.ControlBodySize is var bodySize
        && (this.EffectiveRotatedSize.X > bodySize.X || this.EffectiveRotatedSize.Y > bodySize.Y);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.MouseActivity.Pan -= this.MouseActivityTrackerOnPan;
        this.MouseActivity.DoubleClickDragZoom -= this.MouseActivityTrackerOnDoubleClickDragZoom;
        this.MouseActivity.WheelZoom -= this.MouseActivityTrackerOnWheelZoom;
        this.MouseActivity.LeftDoubleClick -= this.MouseActivityOnLeftDoubleClick;
        this.MouseActivity.DragEnd -= this.MouseActivityOnDragEnd;
        this.Control.MeasuredBoundaryBoxChange -= this.ControlOnMeasuredBoundaryBoxChange;
    }

    /// <summary>Resets to the initial viewport state.</summary>
    /// <param name="revertSize">The size to revert to.</param>
    /// <param name="revertRotation">The rotation to vert to.</param>
    public void Reset(Vector2? revertSize, float? revertRotation)
    {
        var changed = false;

        var prevZoom = this.EffectiveZoom;
        this.scaleMode = null;

        if (this.pan != default)
        {
            changed = true;
            this.pan = default;
        }

        if (revertSize is not null && revertSize.Value != this.size)
        {
            changed = true;
            this.size = revertSize.Value;
        }

        if (revertRotation is not null && !Equals(revertRotation.Value, this.rotation))
        {
            changed = true;
            this.rotation = revertRotation.Value;
        }

        changed |= !Equals(prevZoom, this.EffectiveZoom);

        if (!this.EnforceLimits() && changed)
            this.ViewportChanged?.Invoke();
    }

    /// <summary>Updates the scale mode, using the default origin.</summary>
    /// <param name="mode">New scale mode.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateScaleMode(IRectScaleMode? mode) => this.UpdateScaleMode(mode, this.DefaultRelativeOrigin);

    /// <summary>Updates the scale mode.</summary>
    /// <param name="mode">New scale mode.</param>
    /// <param name="relativeOrigin">The origin of zoom operation.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateScaleMode(IRectScaleMode? mode, Vector2 relativeOrigin)
    {
        if (mode is { } sm)
        {
            if (sm is FreeExponentRectScaleMode fzsm)
            {
                fzsm.ZoomExponent = Math.Clamp(
                    fzsm.ZoomExponent,
                    -this.ZoomExponentDividendRange,
                    +this.ZoomExponentDividendRange);
                mode = fzsm;
            }

            if (Equals(
                    mode.CalcZoom(this.RotatedSize, this.ControlBodySize, this.ZoomExponentDivisor),
                    this.EffectiveZoom))
            {
                this.scaleMode = mode;
                return false;
            }
        }
        else if (this.scaleMode is null)
        {
            if (Equals(this.lastKnownZoom, this.EffectiveZoom))
                return false;

            this.ViewportChanged?.Invoke();
            return true;
        }

        var lo = this.ContentRelativeToLocal(relativeOrigin);

        this.scaleMode = mode;
        this.lastKnownZoom = this.EffectiveZoom;

        var nlo = this.ContentRelativeToLocal(relativeOrigin);

        var dist = lo - nlo;
        if (!this.UpdatePan(this.pan + dist))
            this.ViewportChanged?.Invoke();

        return true;
    }

    /// <summary>Updates the zoom, by providing a new direct ratio value, using the default origin.</summary>
    /// <param name="value">New zoom.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateZoom(float? value) => this.UpdateZoom(value, this.DefaultRelativeOrigin);

    /// <summary>Updates the zoom, by providing a new direct ratio value.</summary>
    /// <param name="value">New zoom.</param>
    /// <param name="relativeOrigin">The origin of zoom operation.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateZoom(float? value, Vector2 relativeOrigin) =>
        this.UpdateScaleMode(
            value is null
                ? null
                : new FreeRectScaleMode(
                    Math.Clamp(
                        value.Value,
                        IRectScaleMode.ExponentToZoom(-this.ZoomExponentDividendRange, this.ZoomExponentDivisor),
                        IRectScaleMode.ExponentToZoom(this.ZoomExponentDividendRange, this.ZoomExponentDivisor))),
            relativeOrigin);

    /// <summary>Updates the zoom, by providing a new exponent, using the default origin.</summary>
    /// <param name="value">New zoom exponent.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateZoomExponent(float? value) => this.UpdateZoomExponent(value, this.DefaultRelativeOrigin);

    /// <summary>Updates the zoom, by providing a new exponent.</summary>
    /// <param name="value">New zoom exponent.</param>
    /// <param name="relativeOrigin">The origin of zoom operation.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateZoomExponent(float? value, Vector2 relativeOrigin) =>
        this.UpdateScaleMode(
            value is null
                ? null
                : new FreeExponentRectScaleMode(
                    Math.Clamp(value.Value, -this.ZoomExponentDividendRange, this.ZoomExponentDividendRange)),
            relativeOrigin);

    /// <summary>Tests if an attempt to pan to a desired point will actually make a difference.</summary>
    /// <param name="desiredPan">The desired panning point.</param>
    /// <param name="adjustedPan">Adjusted <paramref name="desiredPan"/> so that it fits into the boundary.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool WillPanChange(Vector2 desiredPan, out Vector2 adjustedPan)
    {
        adjustedPan = desiredPan;

        if (this.IsPanClampingActive)
        {
            var bodySize = this.ControlBodySize;
            var scaled = this.EffectiveRotatedSize;
            var xrange = DivRem(scaled.X - bodySize.X, 2, out var xrem);
            var yrange = DivRem(scaled.Y - bodySize.Y, 2, out var yrem);
            xrem = MathF.Ceiling(xrem);
            yrem = MathF.Ceiling(yrem);

            if (scaled.X <= bodySize.X)
            {
                adjustedPan.X = 0;
            }
            else
            {
                var minX = -xrange - xrem - this.PanExtraRange.Right;
                var maxX = xrange + this.PanExtraRange.Left;
                adjustedPan.X = Math.Clamp(adjustedPan.X, minX, maxX);
            }

            if (scaled.Y <= bodySize.Y)
            {
                adjustedPan.Y = 0;
            }
            else
            {
                var minY = -yrange - yrem - this.PanExtraRange.Bottom;
                var maxY = yrange + this.PanExtraRange.Top;
                adjustedPan.Y = Math.Clamp(adjustedPan.Y, minY, maxY);
            }
        }

        return adjustedPan != this.pan;
    }

    /// <summary>Updates the panning.</summary>
    /// <param name="desiredPan">The desired panning point.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdatePan(Vector2 desiredPan)
    {
        if (this.IsPanClampingActive)
        {
            var bodySize = this.ControlBodySize;
            var scaled = this.EffectiveRotatedSize;
            var xrange = DivRem(scaled.X - bodySize.X, 2, out var xrem);
            var yrange = DivRem(scaled.Y - bodySize.Y, 2, out var yrem);
            xrem = MathF.Ceiling(xrem);
            yrem = MathF.Ceiling(yrem);

            if (scaled.X <= bodySize.X)
            {
                desiredPan.X = 0;
            }
            else
            {
                var minX = -xrange - xrem - this.PanExtraRange.Right;
                var maxX = xrange + this.PanExtraRange.Left;
                desiredPan.X = Math.Clamp(desiredPan.X, minX, maxX);
            }

            if (scaled.Y <= bodySize.Y)
            {
                desiredPan.Y = 0;
            }
            else
            {
                var minY = -yrange - yrem - this.PanExtraRange.Bottom;
                var maxY = yrange + this.PanExtraRange.Top;
                desiredPan.Y = Math.Clamp(desiredPan.Y, minY, maxY);
            }
        }

        if (desiredPan == this.pan)
            return false;

        this.pan = desiredPan;
        this.ViewportChanged?.Invoke();
        return true;
    }

    /// <summary>Updates the rotation, using the defaut origin.</summary>
    /// <param name="newRotation">A new rotation value in radians.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateRotation(float newRotation) => this.UpdateRotation(newRotation, this.DefaultRelativeOrigin);

    /// <summary>Updates the rotation, using the defaut origin.</summary>
    /// <param name="newRotation">A new clockwise rotation value in radians.</param>
    /// <param name="localOrigin">The origin of rotate operation.</param>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool UpdateRotation(float newRotation, Vector2 localOrigin)
    {
        this.rotation %= MathF.PI * 2;
        if (Equals(newRotation, this.rotation))
            return false;

        var v = this.LocalToContentRelative(localOrigin) * this.size * this.EffectiveZoom;
        var rvr = Vector2.Transform(v, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, this.rotation));
        var rvq = Vector2.Transform(v, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, newRotation));
        var dist = rvr - rvq;

        this.rotation = newRotation;
        if (!this.UpdatePan(this.pan + dist))
            this.ViewportChanged?.Invoke();

        return true;
    }

    /// <summary>Re-applies limits to panning and zooming according to constraints.</summary>
    /// <returns><c>true</c> if effective viewport has changed.</returns>
    public bool EnforceLimits() =>
        this.UpdateScaleMode(this.scaleMode, this.DefaultRelativeOrigin) | this.UpdatePan(this.Pan);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float DivRem(float dividend, float divisor, out float remainder)
    {
        remainder = dividend % divisor;
        return (int)MathF.Floor(dividend / divisor);
    }

    private void MouseActivityTrackerOnWheelZoom(Vector2 localOrigin, float delta)
    {
        var normalizedDelta =
            Math.Sign(delta) * (int)Math.Ceiling(Math.Abs(delta) * this.ZoomExponentDividendWheelDelta);

        if (normalizedDelta == 0)
            return;

        var relativeOrigin = this.LocalToContentRelative(localOrigin);
        if (this.scaleMode is not FreeExponentRectScaleMode freeScaleMode)
        {
            var zoomExponent = normalizedDelta switch
            {
                > 0 => (int)Math.Floor(this.EffectiveZoomExponent),
                < 0 => (int)Math.Ceiling(this.EffectiveZoomExponent),
                _ => throw new InvalidOperationException(),
            };
            this.UpdateZoomExponent(zoomExponent + normalizedDelta, relativeOrigin);
        }
        else
        {
            var effectiveZoom = this.EffectiveZoom;
            var defaultZoom = this.defaultScaleMode.CalcZoom(
                this.RotatedSize,
                this.ControlBodySize,
                this.ZoomExponentDivisor);
            var nextZoom = MathF.Pow(2, (freeScaleMode.ZoomExponent + normalizedDelta) / this.ZoomExponentDivisor);
            if (effectiveZoom < defaultZoom && defaultZoom <= nextZoom)
                this.UpdateScaleMode(null, relativeOrigin);
            else if (nextZoom <= defaultZoom && defaultZoom < effectiveZoom)
                this.UpdateScaleMode(null, relativeOrigin);
            else if (effectiveZoom < 1 && nextZoom >= 1)
                this.UpdateScaleMode(NoZoomRectScaleMode.Instance, relativeOrigin);
            else if (nextZoom <= 1 && effectiveZoom > 1)
                this.UpdateScaleMode(NoZoomRectScaleMode.Instance, relativeOrigin);
            else
                this.UpdateZoomExponent(freeScaleMode.ZoomExponent + normalizedDelta, relativeOrigin);
        }
    }

    private Vector2 LocalToContentRelative(Vector2 localCoordinates)
    {
        var center = this.Control.MeasuredBoundaryBox.Center + this.pan;
        var crcoord = localCoordinates - center;
        crcoord = Vector2.Transform(crcoord, Matrix4x4.CreateRotationZ(-this.rotation));
        crcoord /= this.size * this.EffectiveZoom;
        // Serilog.Log.Information($"L->R: {localCoordinates} => {crcoord}");
        return crcoord;
    }

    private Vector2 ContentRelativeToLocal(Vector2 relativeCoordinates)
    {
        var center = this.Control.MeasuredBoundaryBox.Center + this.pan;
        var lcoord = relativeCoordinates * (this.size * this.EffectiveZoom);
        lcoord = Vector2.Transform(lcoord, Matrix4x4.CreateRotationZ(this.rotation));
        lcoord += center;
        // Serilog.Log.Information($"R->L: {relativeCoordinates} => {lcoord}");
        return lcoord;
    }

    private void MouseActivityTrackerOnDoubleClickDragZoom(Vector2 localOrigin, float delta)
    {
        var exp = (this.MouseActivity.IsLeftHeld ? 1 : 0) +
                  (this.MouseActivity.IsRightHeld ? 1 : 0) +
                  (this.MouseActivity.IsMiddleHeld ? 1 : 0);
        var multiplier = 1 << (exp - 1);
        this.UpdateZoomExponent(
            MathF.Round(
                (this.EffectiveZoomExponent + (delta * this.ZoomExponentDividendDragDelta * multiplier)) *
                this.ZoomExponentDivisor) / this.ZoomExponentDivisor,
            this.LocalToContentRelative(localOrigin));
    }

    private void MouseActivityTrackerOnPan(Vector2 delta)
    {
        switch (this.MouseActivity.FirstHeldButton)
        {
            case ImGuiMouseButton.Left:
                this.UpdatePan(this.Pan + (delta * this.PanSpeedMultiplier));
                break;
            case ImGuiMouseButton.Right:
                this.UpdateRotation(
                    ((((this.rotation * 180) / MathF.PI) + (delta.X + delta.Y)) * MathF.PI) / 180,
                    this.MouseActivity.DragOrigin ?? this.ContentRelativeToLocal(this.DefaultRelativeOrigin));
                break;
            case ImGuiMouseButton.Middle:
                this.MouseActivityTrackerOnDoubleClickDragZoom(
                    this.MouseActivity.DragOrigin ?? this.ContentRelativeToLocal(this.DefaultRelativeOrigin),
                    delta.Y);
                break;
        }
    }

    private void MouseActivityOnLeftDoubleClick(Vector2 location)
    {
        var fillingZoom = FitInClientRectScaleMode.CalcZoomStatic(this.RotatedSize, this.ControlBodySize, true);
        IRectScaleMode? newMode = fillingZoom switch
        {
            < 1 => this.scaleMode is null ? NoZoomRectScaleMode.Instance : null,
            > 1 => this.EffectiveZoom * 2 < 1 + fillingZoom ? FitInClientRectScaleMode.GetInstance(true) : null,
            _ => null,
        };
        this.UpdateScaleMode(newMode, this.LocalToContentRelative(location));
    }

    private void MouseActivityOnDragEnd()
    {
        this.UpdatePan(new((int)this.Pan.X, (int)this.Pan.Y));
        this.UpdateRotation(
            (MathF.Round((this.rotation * 180) / MathF.PI / 15) * 15 * MathF.PI) / 180,
            this.MouseActivity.DragOrigin ?? this.ContentRelativeToLocal(this.DefaultRelativeOrigin));
    }

    private void ControlOnMeasuredBoundaryBoxChange(PropertyChangeEventArgs<ControlSpannable, RectVector4> args)
    {
        if (args.State != PropertyChangeState.After)
            return;
        if (!this.EnforceLimits())
            this.ViewportChanged?.Invoke();
    }
}

using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class ControlSpannable
{
    /// <inheritdoc/>
    public event Action<ISpannable>? SpannableChange;

    /// <summary>Occurs when the control should handle interactions.</summary>
    public event SpannableControlEventHandler? HandleInteraction;

    /// <summary>Occurs when the control receives new transformation parameters.</summary>
    public event SpannableControlEventHandler? UpdateTransformation;

    /// <summary>Occurs when the control is clicked.</summary>
    public event SpannableControlEventHandler? Click;

    /// <summary>Occurs when the control is clicked by the mouse.</summary>
    public event ControlDrawEventHandler? Draw;

    /// <summary>Occurs when the control receives focus.</summary>
    public event SpannableControlEventHandler? GotFocus;

    /// <summary>Occurs when the control loses focus.</summary>
    public event SpannableControlEventHandler? LostFocus;

    /// <summary>Occurs when the control is clicked by the mouse.</summary>
    public event ControlMouseEventHandler? MouseClick;

    /// <summary>Occurs when the control is held down for a duration that would start repeated key press events.
    /// </summary>
    public event ControlMouseEventHandler? MousePressLong;

    /// <summary>Occurs when the control has been held down for a duration and is repeatedly generating events like
    /// <see cref="KeyPress"/>.</summary>
    public event ControlMouseEventHandler? MousePressRepeat;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is pressed.</summary>
    public event ControlMouseEventHandler? MouseDown;

    /// <summary>Occurs when the mouse pointer enters the control.</summary>
    public event ControlMouseEventHandler? MouseEnter;

    /// <summary>Occurs when the mouse pointer leaves the control.</summary>
    public event ControlMouseEventHandler? MouseLeave;

    /// <summary>Occurs when the mouse pointer is moved over the control.</summary>
    public event ControlMouseEventHandler? MouseMove;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is released.</summary>
    public event ControlMouseEventHandler? MouseUp;

    /// <summary>Occurs when the mouse wheel moves while the control is hovered.</summary>
    public event ControlMouseEventHandler? MouseWheel;

    /// <summary>Occurs when a key is pressed while the control has focus.</summary>
    public event ControlKeyEventHandler? KeyDown;

    /// <summary>Occurs when a character, space or backspace key is pressed while the control has focus.</summary>
    public event ControlKeyPressEventHandler? KeyPress;

    /// <summary>Occurs when a key is released while the control has focus.</summary>
    public event ControlKeyEventHandler? KeyUp;

    /// <summary>Occurs when the property <see cref="MeasuredOutsideBox"/> is changing.</summary>
    public event PropertyChangeEventHandler<RectVector4>? MeasuredOutsideBoxChange;

    /// <summary>Occurs when the property <see cref="MeasuredBoundaryBox"/> is changing.</summary>
    public event PropertyChangeEventHandler<RectVector4>? MeasuredBoundaryBoxChange;

    /// <summary>Occurs when the property <see cref="MeasuredInteractiveBox"/> is changing.</summary>
    public event PropertyChangeEventHandler<RectVector4>? MeasuredInteractiveBoxChange;

    /// <summary>Occurs when the property <see cref="MeasuredContentBox"/> is changing.</summary>
    public event PropertyChangeEventHandler<RectVector4>? MeasuredContentBoxChange;

    /// <summary>Occurs when the property <see cref="Name"/> is changing.</summary>
    public event PropertyChangeEventHandler<string>? NameChange;

    /// <summary>Occurs when the property <see cref="Enabled"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? EnabledChange;

    /// <summary>Occurs when the property <see cref="Focusable"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? FocusableChange;

    /// <summary>Occurs when the property <see cref="Visible"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? VisibleChange;

    /// <summary>Occurs when the property <see cref="ClipChildren"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? ClipChildrenChange;

    /// <summary>Occurs when the property <see cref="MouseCursor"/> is changing.</summary>
    public event PropertyChangeEventHandler<ImGuiMouseCursor>? MouseCursorChange;

    /// <summary>Occurs when the property <see cref="Text"/> is changing.</summary>
    public event PropertyChangeEventHandler<string?>? TextChange;

    /// <summary>Occurs when the property <see cref="TextStyle"/> is changing.</summary>
    public event PropertyChangeEventHandler<TextStyle>? TextStyleChange;

    /// <summary>Occurs when the property <see cref="Scale"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? ScaleChange;

    /// <summary>Occurs when the property <see cref="RenderScale"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? RenderScaleChange;

    /// <summary>Occurs when the property <see cref="Size"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? SizeChange;

    /// <summary>Occurs when the property <see cref="ExtendOutside"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? ExtendOutsideChange;

    /// <summary>Occurs when the property <see cref="Margin"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? MarginChange;

    /// <summary>Occurs when the property <see cref="Padding"/> is changing.</summary>
    public event PropertyChangeEventHandler<BorderVector4>? PaddingChange;

    /// <summary>Occurs when the property <see cref="Transformation"/> is changing.</summary>
    public event PropertyChangeEventHandler<Matrix4x4>? TransformationChange;

    /// <summary>Occurs when the property <see cref="NormalBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? NormalBackgroundChange;

    /// <summary>Occurs when the property <see cref="HoveredBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? HoveredBackgroundChange;

    /// <summary>Occurs when the property <see cref="ActiveBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? ActiveBackgroundChange;

    /// <summary>Occurs when the property <see cref="DisabledBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannable?>? DisabledBackgroundChange;

    /// <summary>Occurs when the property <see cref="ShowAnimation"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableAnimator?>? ShowAnimationChange;

    /// <summary>Occurs when the property <see cref="HideAnimation"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableAnimator?>? HideAnimationChange;

    /// <summary>Occurs when the property <see cref="MoveAnimation"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableAnimator?>? MoveAnimationChange;

    /// <summary>Occurs when the property <see cref="TransformationChangeAnimationChange"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableAnimator?>? TransformationChangeAnimationChange;

    /// <summary>Occurs when the property <see cref="DisabledTextOpacity"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? DisabledTextOpacityChange;

    /// <summary>Occurs when the property <see cref="CaptureMouseOnMouseDown"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CaptureMouseOnMouseDownChange;

    /// <summary>Occurs when the property <see cref="CaptureMouseWheel"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CaptureMouseWheelChange;

    /// <summary>Occurs when the property <see cref="CaptureMouse"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? CaptureMouseChange;

    /// <summary>Occurs when the property <see cref="TakeKeyboardInputsOnFocus"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? TakeKeyboardInputsOnFocusChange;

    /// <summary>Raises the <see cref="SpannableChange"/> event.</summary>
    /// <param name="obj">The spannable that has been changed.</param>
    protected virtual void OnSpannableChange(ISpannable obj)
    {
        this.IsMeasurementValid = false;
        this.SpannableChange?.Invoke(obj);
    }

    /// <summary>Raises the <see cref="HandleInteraction"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnHandleInteraction(SpannableEventArgs args) => this.HandleInteraction?.Invoke(args);

    /// <summary>Raises the <see cref="UpdateTransformation"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnUpdateTransformation(SpannableEventArgs args) =>
        this.UpdateTransformation?.Invoke(args);

    /// <summary>Raises the <see cref="Click"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnClick(SpannableEventArgs args) => this.Click?.Invoke(args);

    /// <summary>Raises the <see cref="Draw"/> event.</summary>
    /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDraw(SpannableDrawEventArgs args) => this.Draw?.Invoke(args);

    /// <summary>Raises the <see cref="GotFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnGotFocus(SpannableEventArgs args) => this.GotFocus?.Invoke(args);

    /// <summary>Raises the <see cref="LostFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnLostFocus(SpannableEventArgs args) => this.LostFocus?.Invoke(args);

    /// <summary>Raises the <see cref="MouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseClick(SpannableMouseEventArgs args)
    {
        this.MouseClick?.Invoke(args);
        this.OnClick(args);
    }

    /// <summary>Raises the <see cref="MousePressLong"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMousePressLong(SpannableMouseEventArgs args) => this.MousePressLong?.Invoke(args);

    /// <summary>Raises the <see cref="MousePressRepeat"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMousePressRepeat(SpannableMouseEventArgs args) => this.MousePressRepeat?.Invoke(args);

    /// <summary>Raises the <see cref="MouseDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseDown(SpannableMouseEventArgs args) => this.MouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="MouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseEnter(SpannableMouseEventArgs args) => this.MouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="MouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseLeave(SpannableMouseEventArgs args) => this.MouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="MouseMove"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseMove(SpannableMouseEventArgs args) => this.MouseMove?.Invoke(args);

    /// <summary>Raises the <see cref="MouseUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseUp(SpannableMouseEventArgs args) => this.MouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="MouseWheel"/> event.</summary>
    /// <param name="args">A <see cref="SpannableMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseWheel(SpannableMouseEventArgs args) => this.MouseWheel?.Invoke(args);

    /// <summary>Raises the <see cref="KeyDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableKeyEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyDown(SpannableKeyEventArgs args) => this.KeyDown?.Invoke(args);

    /// <summary>Raises the <see cref="KeyPress"/> event.</summary>
    /// <param name="args">A <see cref="SpannableKeyPressEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyPress(SpannableKeyPressEventArgs args)
    {
        this.KeyPress?.Invoke(args);
        if (!args.Handled && args.KeyChar == 13)
            this.OnClick(args);
    }

    /// <summary>Raises the <see cref="KeyUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableKeyEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyUp(SpannableKeyEventArgs args) => this.KeyUp?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredOutsideBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMeasuredOutsideBoxChange(PropertyChangeEventArgs<RectVector4> args) =>
        this.MeasuredOutsideBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredBoundaryBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMeasuredBoundaryBoxChange(PropertyChangeEventArgs<RectVector4> args) =>
        this.MeasuredBoundaryBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredInteractiveBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void
        OnMeasuredInteractiveBoxChange(PropertyChangeEventArgs<RectVector4> args) =>
        this.MeasuredInteractiveBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredContentBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMeasuredContentBoxChange(PropertyChangeEventArgs<RectVector4> args) =>
        this.MeasuredContentBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="NameChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNameChange(PropertyChangeEventArgs<string> args) =>
        this.NameChange?.Invoke(args);

    /// <summary>Raises the <see cref="EnabledChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnEnabledChange(PropertyChangeEventArgs<bool> args) =>
        this.EnabledChange?.Invoke(args);

    /// <summary>Raises the <see cref="FocusableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnFocusableChange(PropertyChangeEventArgs<bool> args) =>
        this.FocusableChange?.Invoke(args);

    /// <summary>Raises the <see cref="VisibleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnVisibleChange(PropertyChangeEventArgs<bool> args)
    {
        this.VisibleChange?.Invoke(args);
        if (args.State != PropertyChangeState.After)
            this.localTransformationDirectBefore = default;
    }

    /// <summary>Raises the <see cref="ClipChildrenChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnClipChildrenChange(PropertyChangeEventArgs<bool> args) =>
        this.ClipChildrenChange?.Invoke(args);

    /// <summary>Raises the <see cref="MouseCursorChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMouseCursorChange(PropertyChangeEventArgs<ImGuiMouseCursor> args) =>
        this.MouseCursorChange?.Invoke(args);

    /// <summary>Raises the <see cref="TextChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTextChange(PropertyChangeEventArgs<string?> args) =>
        this.TextChange?.Invoke(args);

    /// <summary>Raises the <see cref="TextStyleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTextStyleChange(PropertyChangeEventArgs<TextStyle> args)
        => this.TextStyleChange?.Invoke(args);

    /// <summary>Raises the <see cref="ScaleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnScaleChange(PropertyChangeEventArgs<float> args) =>
        this.ScaleChange?.Invoke(args);

    /// <summary>Raises the <see cref="RenderScaleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnRenderScaleChange(PropertyChangeEventArgs<float> args) =>
        this.RenderScaleChange?.Invoke(args);

    /// <summary>Raises the <see cref="SizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSizeChange(PropertyChangeEventArgs<Vector2> args) =>
        this.SizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="ExtendOutsideChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnExtendOutsideChange(PropertyChangeEventArgs<BorderVector4> args) =>
        this.ExtendOutsideChange?.Invoke(args);

    /// <summary>Raises the <see cref="MarginChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMarginChange(PropertyChangeEventArgs<BorderVector4> args) =>
        this.MarginChange?.Invoke(args);

    /// <summary>Raises the <see cref="PaddingChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnPaddingChange(PropertyChangeEventArgs<BorderVector4> args) =>
        this.PaddingChange?.Invoke(args);

    /// <summary>Raises the <see cref="TransformationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTransformationChange(PropertyChangeEventArgs<Matrix4x4> args)
    {
        this.TransformationChange?.Invoke(args);

        if (args.State != PropertyChangeState.After)
            return;

        if (this.transformationChangeAnimation is not null)
        {
            this.transformationChangeAnimation.Update(this);
            if (this.transformationChangeAnimation.AfterMatrix != this.transformation && !this.suppressNextAnimation)
            {
                this.transformationChangeAnimation.AfterMatrix = this.transformation;
                this.transformationChangeAnimation.BeforeMatrix
                    = this.transformationChangeAnimation.IsRunning
                          ? this.transformationChangeAnimation.AnimatedTransformation
                          : args.PreviousValue;
                this.transformationChangeAnimation.Restart();
                this.transformationChangeAnimation.Update(this);
            }

            this.localTransformation = Matrix4x4.Multiply(
                this.localTransformation,
                this.transformationChangeAnimation.IsRunning
                    ? this.transformationChangeAnimation.AnimatedTransformation
                    : args.NewValue);
        }

        if (!this.suppressNextAnimation)
            this.transformationChangeAnimation?.Restart();
    }

    /// <summary>Raises the <see cref="NormalBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNormalBackgroundChange(PropertyChangeEventArgs<ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.normalBackgroundChildIndex] = args.NewValue;
        this.NormalBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="HoveredBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHoveredBackgroundChange(PropertyChangeEventArgs<ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.hoveredBackgroundChildIndex] = args.NewValue;
        this.HoveredBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ActiveBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnActiveBackgroundChange(PropertyChangeEventArgs<ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.activeBackgroundChildIndex] = args.NewValue;
        this.ActiveBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="DisabledBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDisabledBackgroundChange(PropertyChangeEventArgs<ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.disabledBackgroundChildIndex] = args.NewValue;
        this.DisabledBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ShowAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnShowAnimationChange(
        PropertyChangeEventArgs<SpannableAnimator?> args) =>
        this.ShowAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="HideAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHideAnimationChange(
        PropertyChangeEventArgs<SpannableAnimator?> args) =>
        this.HideAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="MoveAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMoveAnimationChange(
        PropertyChangeEventArgs<SpannableAnimator?> args) =>
        this.MoveAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="TransformationChangeAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTransformationChangeAnimationChange(
        PropertyChangeEventArgs<SpannableAnimator?> args) =>
        this.TransformationChangeAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="DisabledTextOpacityChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDisabledTextOpacityChange(PropertyChangeEventArgs<float> args) =>
        this.DisabledTextOpacityChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseOnMouseDown"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseOnMouseDownChange(PropertyChangeEventArgs<bool> args) =>
        this.CaptureMouseOnMouseDownChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseWheelChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseWheelChange(PropertyChangeEventArgs<bool> args) =>
        this.CaptureMouseWheelChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseChange(PropertyChangeEventArgs<bool> args) =>
        this.CaptureMouseChange?.Invoke(args);

    /// <summary>Raises the <see cref="TakeKeyboardInputsOnFocusChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTakeKeyboardInputsOnFocusChange(PropertyChangeEventArgs<bool> args) =>
        this.TakeKeyboardInputsOnFocusChange?.Invoke(args);
}

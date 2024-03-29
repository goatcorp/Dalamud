using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

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

    /// <summary>Occurs when the property <see cref="MeasuredOutsideBox"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, RectVector4>? MeasuredOutsideBoxChange;

    /// <summary>Occurs when the property <see cref="MeasuredBoundaryBox"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, RectVector4>? MeasuredBoundaryBoxChange;

    /// <summary>Occurs when the property <see cref="MeasuredInteractiveBox"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, RectVector4>? MeasuredInteractiveBoxChange;

    /// <summary>Occurs when the property <see cref="MeasuredContentBox"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, RectVector4>? MeasuredContentBoxChange;

    /// <summary>Occurs when the property <see cref="Name"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, string>? NameChange;

    /// <summary>Occurs when the property <see cref="Enabled"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? EnabledChange;

    /// <summary>Occurs when the property <see cref="Focusable"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? FocusableChange;

    /// <summary>Occurs when the property <see cref="Visible"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? VisibleChange;

    /// <summary>Occurs when the property <see cref="ClipChildren"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? ClipChildrenChange;

    /// <summary>Occurs when the property <see cref="Text"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, string?>? TextChange;

    /// <summary>Occurs when the property <see cref="TextStyle"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, TextStyle>? TextStyleChange;

    /// <summary>Occurs when the property <see cref="Scale"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, float>? ScaleChange;

    /// <summary>Occurs when the property <see cref="RenderScale"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, float>? RenderScaleChange;

    /// <summary>Occurs when the property <see cref="Size"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, Vector2>? SizeChange;

    /// <summary>Occurs when the property <see cref="MinSize"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, Vector2>? MinSizeChange;

    /// <summary>Occurs when the property <see cref="MaxSize"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, Vector2>? MaxSizeChange;

    /// <summary>Occurs when the property <see cref="ExtendOutside"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, BorderVector4>? ExtendOutsideChange;

    /// <summary>Occurs when the property <see cref="Margin"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, BorderVector4>? MarginChange;

    /// <summary>Occurs when the property <see cref="Padding"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, BorderVector4>? PaddingChange;

    /// <summary>Occurs when the property <see cref="Transformation"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, Matrix4x4>? TransformationChange;

    /// <summary>Occurs when the property <see cref="NormalBackground"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? NormalBackgroundChange;

    /// <summary>Occurs when the property <see cref="HoveredBackground"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? HoveredBackgroundChange;

    /// <summary>Occurs when the property <see cref="ActiveBackground"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? ActiveBackgroundChange;

    /// <summary>Occurs when the property <see cref="DisabledBackground"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, ISpannable?>? DisabledBackgroundChange;

    /// <summary>Occurs when the property <see cref="ShowAnimation"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, SpannableAnimator?>? ShowAnimationChange;

    /// <summary>Occurs when the property <see cref="HideAnimation"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, SpannableAnimator?>? HideAnimationChange;

    /// <summary>Occurs when the property <see cref="MoveAnimation"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, SpannableAnimator?>? MoveAnimationChange;

    /// <summary>Occurs when the property <see cref="TransformationChangeAnimationChange"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, SpannableAnimator?>? TransformationChangeAnimationChange;

    /// <summary>Occurs when the property <see cref="DisabledTextOpacity"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, float>? DisabledTextOpacityChange;

    /// <summary>Occurs when the property <see cref="CaptureMouseOnMouseDown"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? CaptureMouseOnMouseDownChange;

    /// <summary>Occurs when the property <see cref="CaptureMouseWheel"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? CaptureMouseWheelChange;

    /// <summary>Occurs when the property <see cref="CaptureMouse"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? CaptureMouseChange;

    /// <summary>Occurs when the property <see cref="TakeKeyboardInputsOnFocus"/> has been changed.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? TakeKeyboardInputsOnFocusChange;

    /// <summary>Raises the <see cref="SpannableChange"/> event.</summary>
    /// <param name="obj">The spannable that has been changed.</param>
    protected virtual void OnSpannableChange(ISpannable obj)
    {
        this.IsMeasurementValid = false;
        this.SpannableChange?.Invoke(obj);
    }

    /// <summary>Raises the <see cref="HandleInteraction"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlEventArgs"/> that contains the event data.</param>
    protected virtual void OnHandleInteraction(SpannableControlEventArgs args) => this.HandleInteraction?.Invoke(args);

    /// <summary>Raises the <see cref="UpdateTransformation"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlEventArgs"/> that contains the event data.</param>
    protected virtual void OnUpdateTransformation(SpannableControlEventArgs args) =>
        this.UpdateTransformation?.Invoke(args);

    /// <summary>Raises the <see cref="Click"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlEventArgs"/> that contains the event data.</param>
    protected virtual void OnClick(SpannableControlEventArgs args) => this.Click?.Invoke(args);

    /// <summary>Raises the <see cref="Draw"/> event.</summary>
    /// <param name="args">A <see cref="ControlDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDraw(ControlDrawEventArgs args) => this.Draw?.Invoke(args);

    /// <summary>Raises the <see cref="GotFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlEventArgs"/> that contains the event data.</param>
    protected virtual void OnGotFocus(SpannableControlEventArgs args) => this.GotFocus?.Invoke(args);

    /// <summary>Raises the <see cref="LostFocus"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlEventArgs"/> that contains the event data.</param>
    protected virtual void OnLostFocus(SpannableControlEventArgs args) => this.LostFocus?.Invoke(args);

    /// <summary>Raises the <see cref="MouseClick"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseClick(ControlMouseEventArgs args)
    {
        this.MouseClick?.Invoke(args);
        this.OnClick(args);
    }

    /// <summary>Raises the <see cref="MouseDown"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseDown(ControlMouseEventArgs args) => this.MouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="MouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseEnter(ControlMouseEventArgs args) => this.MouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="MouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseLeave(ControlMouseEventArgs args) => this.MouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="MouseMove"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseMove(ControlMouseEventArgs args) => this.MouseMove?.Invoke(args);

    /// <summary>Raises the <see cref="MouseUp"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseUp(ControlMouseEventArgs args) => this.MouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="MouseWheel"/> event.</summary>
    /// <param name="args">A <see cref="ControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseWheel(ControlMouseEventArgs args) => this.MouseWheel?.Invoke(args);

    /// <summary>Raises the <see cref="KeyDown"/> event.</summary>
    /// <param name="args">A <see cref="ControlKeyEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyDown(ControlKeyEventArgs args) => this.KeyDown?.Invoke(args);

    /// <summary>Raises the <see cref="KeyPress"/> event.</summary>
    /// <param name="args">A <see cref="ControlKeyPressEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyPress(ControlKeyPressEventArgs args)
    {
        this.KeyPress?.Invoke(args);
        if (!args.Handled && args.KeyChar == 13)
            this.OnClick(args);
    }

    /// <summary>Raises the <see cref="KeyUp"/> event.</summary>
    /// <param name="args">A <see cref="ControlKeyEventArgs"/> that contains the event data.</param>
    protected virtual void OnKeyUp(ControlKeyEventArgs args) => this.KeyUp?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredOutsideBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMeasuredOutsideBoxChange(PropertyChangeEventArgs<ControlSpannable, RectVector4> args) =>
        this.MeasuredOutsideBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredBoundaryBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMeasuredBoundaryBoxChange(PropertyChangeEventArgs<ControlSpannable, RectVector4> args) =>
        this.MeasuredBoundaryBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredInteractiveBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void
        OnMeasuredInteractiveBoxChange(PropertyChangeEventArgs<ControlSpannable, RectVector4> args) =>
        this.MeasuredInteractiveBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="MeasuredContentBoxChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMeasuredContentBoxChange(PropertyChangeEventArgs<ControlSpannable, RectVector4> args) =>
        this.MeasuredContentBoxChange?.Invoke(args);

    /// <summary>Raises the <see cref="NameChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnNameChange(PropertyChangeEventArgs<ControlSpannable, string> args) =>
        this.NameChange?.Invoke(args);

    /// <summary>Raises the <see cref="EnabledChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnEnabledChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.EnabledChange?.Invoke(args);

    /// <summary>Raises the <see cref="FocusableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnFocusableChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.FocusableChange?.Invoke(args);

    /// <summary>Raises the <see cref="VisibleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnVisibleChange(PropertyChangeEventArgs<ControlSpannable, bool> args)
    {
        this.VisibleChange?.Invoke(args);
        if (args.State != PropertyChangeState.After)
            this.localTransformationDirectBefore = default;
    }

    /// <summary>Raises the <see cref="ClipChildrenChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnClipChildrenChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.ClipChildrenChange?.Invoke(args);

    /// <summary>Raises the <see cref="TextChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnTextChange(PropertyChangeEventArgs<ControlSpannable, string?> args) =>
        this.TextChange?.Invoke(args);

    /// <summary>Raises the <see cref="TextStyleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnTextStyleChange(PropertyChangeEventArgs<ControlSpannable, TextStyle> args)
        => this.TextStyleChange?.Invoke(args);

    /// <summary>Raises the <see cref="ScaleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnScaleChange(PropertyChangeEventArgs<ControlSpannable, float> args) =>
        this.ScaleChange?.Invoke(args);

    /// <summary>Raises the <see cref="RenderScaleChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnRenderScaleChange(PropertyChangeEventArgs<ControlSpannable, float> args) =>
        this.RenderScaleChange?.Invoke(args);

    /// <summary>Raises the <see cref="SizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnSizeChange(PropertyChangeEventArgs<ControlSpannable, Vector2> args) =>
        this.SizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="MinSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMinSizeChange(PropertyChangeEventArgs<ControlSpannable, Vector2> args) =>
        this.MinSizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="MaxSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMaxSizeChange(PropertyChangeEventArgs<ControlSpannable, Vector2> args) =>
        this.MaxSizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="ExtendOutsideChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnExtendOutsideChange(PropertyChangeEventArgs<ControlSpannable, BorderVector4> args) =>
        this.ExtendOutsideChange?.Invoke(args);

    /// <summary>Raises the <see cref="MarginChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMarginChange(PropertyChangeEventArgs<ControlSpannable, BorderVector4> args) =>
        this.MarginChange?.Invoke(args);

    /// <summary>Raises the <see cref="PaddingChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnPaddingChange(PropertyChangeEventArgs<ControlSpannable, BorderVector4> args) =>
        this.PaddingChange?.Invoke(args);

    /// <summary>Raises the <see cref="TransformationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnTransformationChange(PropertyChangeEventArgs<ControlSpannable, Matrix4x4> args)
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
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnNormalBackgroundChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.normalBackgroundChildIndex] = args.NewValue;
        this.NormalBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="HoveredBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnHoveredBackgroundChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.hoveredBackgroundChildIndex] = args.NewValue;
        this.HoveredBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ActiveBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnActiveBackgroundChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.activeBackgroundChildIndex] = args.NewValue;
        this.ActiveBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="DisabledBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnDisabledBackgroundChange(PropertyChangeEventArgs<ControlSpannable, ISpannable?> args)
    {
        if (args.State == PropertyChangeState.After)
            this.AllSpannables[this.disabledBackgroundChildIndex] = args.NewValue;
        this.DisabledBackgroundChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ShowAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnShowAnimationChange(
        PropertyChangeEventArgs<ControlSpannable, SpannableAnimator?> args) =>
        this.ShowAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="HideAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnHideAnimationChange(
        PropertyChangeEventArgs<ControlSpannable, SpannableAnimator?> args) =>
        this.HideAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="MoveAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnMoveAnimationChange(
        PropertyChangeEventArgs<ControlSpannable, SpannableAnimator?> args) =>
        this.MoveAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="TransformationChangeAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnTransformationChangeAnimationChange(
        PropertyChangeEventArgs<ControlSpannable, SpannableAnimator?> args) =>
        this.TransformationChangeAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="DisabledTextOpacityChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnDisabledTextOpacityChange(PropertyChangeEventArgs<ControlSpannable, float> args) =>
        this.DisabledTextOpacityChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseOnMouseDown"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseOnMouseDownChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.CaptureMouseOnMouseDownChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseWheelChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseWheelChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.CaptureMouseWheelChange?.Invoke(args);

    /// <summary>Raises the <see cref="CaptureMouseChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnCaptureMouseChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.CaptureMouseChange?.Invoke(args);

    /// <summary>Raises the <see cref="TakeKeyboardInputsOnFocusChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T, TSender}"/> that contains the event data.</param>
    protected virtual void OnTakeKeyboardInputsOnFocusChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.TakeKeyboardInputsOnFocusChange?.Invoke(args);
}

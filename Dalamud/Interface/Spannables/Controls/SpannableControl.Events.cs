using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;
using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class SpannableControl
{
    /// <summary>Occurs when the control obtained the final layout parameters for the render pass.</summary>
    public event SpannableControlMeasureEventHandler? CommitMeasurement;

    /// <summary>Occurs when the control should handle interactions.</summary>
    public event SpannableControlHandleInteractionEventHandler? HandleInteraction;

    /// <summary>Occurs when the control is clicked by the mouse.</summary>
    public event SpannableControlDrawEventHandler? Draw;

    /// <summary>Occurs when the control is clicked by the mouse.</summary>
    public event SpannableControlMouseEventHandler? MouseClick;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is pressed.</summary>
    public event SpannableControlMouseEventHandler? MouseDown;

    /// <summary>Occurs when the mouse pointer enters the control.</summary>
    public event SpannableControlMouseEventHandler? MouseEnter;

    /// <summary>Occurs when the mouse pointer leaves the control.</summary>
    public event SpannableControlMouseEventHandler? MouseLeave;

    /// <summary>Occurs when the mouse pointer is moved over the control.</summary>
    public event SpannableControlMouseEventHandler? MouseMove;

    /// <summary>Occurs when the mouse pointer is over the control and a mouse button is released.</summary>
    public event SpannableControlMouseEventHandler? MouseUp;

    /// <summary>Occurs when the mouse wheel moves while the control is hovered.</summary>
    public event SpannableControlMouseEventHandler? MouseWheel;

    /// <summary>Occurs when the property <see cref="Enabled"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<bool>? EnabledChanged;

    /// <summary>Occurs when the property <see cref="Visible"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<bool>? VisibleChanged;

    /// <summary>Occurs when the property <see cref="Text"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<string?>? TextChanged;

    /// <summary>Occurs when the property <see cref="Size"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<Vector2>? SizeChanged;

    /// <summary>Occurs when the property <see cref="MinSize"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<Vector2>? MinSizeChanged;

    /// <summary>Occurs when the property <see cref="MaxSize"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<Vector2>? MaxSizeChanged;

    /// <summary>Occurs when the property <see cref="Extrude"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<RectVector4>? ExtrudeChanged;

    /// <summary>Occurs when the property <see cref="Margin"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<RectVector4>? MarginChanged;

    /// <summary>Occurs when the property <see cref="Padding"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<RectVector4>? PaddingChanged;

    /// <summary>Occurs when the property <see cref="NormalBackground"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<ISpannable?>? NormalBackgroundChanged;

    /// <summary>Occurs when the property <see cref="HoveredBackground"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<ISpannable?>? HoveredBackgroundChanged;

    /// <summary>Occurs when the property <see cref="ActiveBackground"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<ISpannable?>? ActiveBackgroundChanged;

    /// <summary>Occurs when the property <see cref="DisabledBackground"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<ISpannable?>? DisabledBackgroundChanged;

    /// <summary>Occurs when the property <see cref="ShowAnimation"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<SpannableControlAnimator?>? ShowAnimationChanged;

    /// <summary>Occurs when the property <see cref="HideAnimation"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<SpannableControlAnimator?>? HideAnimationChanged;

    /// <summary>Occurs when the property <see cref="DisabledTextOpacity"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<float>? DisabledTextOpacityChanged;

    /// <summary>Occurs when the property <see cref="InterceptMouseWheelUp"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<bool>? InterceptMouseWheelUpChanged;

    /// <summary>Occurs when the property <see cref="InterceptMouseWheelDown"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<bool>? InterceptMouseWheelDownChanged;

    /// <summary>Occurs when the property <see cref="InterceptMouseWheelLeft"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<bool>? InterceptMouseWheelLeftChanged;

    /// <summary>Occurs when the property <see cref="InterceptMouseWheelRight"/> has been changed.</summary>
    public event SpannableControlPropertyChangedEventHandler<bool>? InterceptMouseWheelRightChanged;

    /// <summary>Raises the <see cref="CommitMeasurement"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlCommitMeasurementArgs"/> that contains the event data.</param>
    protected virtual void OnCommitMeasurement(SpannableControlCommitMeasurementArgs args) =>
        this.CommitMeasurement?.Invoke(args);

    /// <summary>Raises the <see cref="HandleInteraction"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlHandleInteractionArgs"/> that contains the event data.</param>
    /// <param name="link">The interacted link, if any.</param>
    protected virtual void OnHandleInteraction(
        SpannableControlHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        link = default;
        this.HandleInteraction?.Invoke(args, out link);
    }

    /// <summary>Raises the <see cref="Draw"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlDrawArgs"/> that contains the event data.</param>
    protected virtual void OnDraw(SpannableControlDrawArgs args) => this.Draw?.Invoke(args);

    /// <summary>Raises the <see cref="MouseClick"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseClick(SpannableControlMouseEventArgs args) => this.MouseClick?.Invoke(args);

    /// <summary>Raises the <see cref="MouseDown"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseDown(SpannableControlMouseEventArgs args) => this.MouseDown?.Invoke(args);

    /// <summary>Raises the <see cref="MouseEnter"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseEnter(SpannableControlMouseEventArgs args) => this.MouseEnter?.Invoke(args);

    /// <summary>Raises the <see cref="MouseLeave"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseLeave(SpannableControlMouseEventArgs args) => this.MouseLeave?.Invoke(args);

    /// <summary>Raises the <see cref="MouseMove"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseMove(SpannableControlMouseEventArgs args) => this.MouseMove?.Invoke(args);

    /// <summary>Raises the <see cref="MouseUp"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseUp(SpannableControlMouseEventArgs args) => this.MouseUp?.Invoke(args);

    /// <summary>Raises the <see cref="MouseWheel"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlMouseEventArgs"/> that contains the event data.</param>
    protected virtual void OnMouseWheel(SpannableControlMouseEventArgs args) => this.MouseWheel?.Invoke(args);

    /// <summary>Raises the <see cref="EnabledChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnEnabledChanged(SpannableControlPropertyChangedEventArgs<bool> args) =>
        this.EnabledChanged?.Invoke(args);

    /// <summary>Raises the <see cref="VisibleChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnVisibleChanged(SpannableControlPropertyChangedEventArgs<bool> args) =>
        this.VisibleChanged?.Invoke(args);

    /// <summary>Raises the <see cref="TextChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnTextChanged(SpannableControlPropertyChangedEventArgs<string?> args) =>
        this.TextChanged?.Invoke(args);

    /// <summary>Raises the <see cref="SizeChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSizeChanged(SpannableControlPropertyChangedEventArgs<Vector2> args) =>
        this.SizeChanged?.Invoke(args);

    /// <summary>Raises the <see cref="MinSizeChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMinSizeChanged(SpannableControlPropertyChangedEventArgs<Vector2> args) =>
        this.MinSizeChanged?.Invoke(args);

    /// <summary>Raises the <see cref="MaxSizeChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMaxSizeChanged(SpannableControlPropertyChangedEventArgs<Vector2> args) =>
        this.MaxSizeChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ExtrudeChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnExtrudeChanged(SpannableControlPropertyChangedEventArgs<RectVector4> args) =>
        this.ExtrudeChanged?.Invoke(args);

    /// <summary>Raises the <see cref="MarginChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMarginChanged(SpannableControlPropertyChangedEventArgs<RectVector4> args) =>
        this.MarginChanged?.Invoke(args);

    /// <summary>Raises the <see cref="PaddingChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnPaddingChanged(SpannableControlPropertyChangedEventArgs<RectVector4> args) =>
        this.PaddingChanged?.Invoke(args);

    /// <summary>Raises the <see cref="NormalBackgroundChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNormalBackgroundChanged(SpannableControlPropertyChangedEventArgs<ISpannable?> args)
    {
        this.AllChildren[this.normalBackgroundChildIndex] = args.NewValue;
        this.NormalBackgroundChanged?.Invoke(args);
    }

    /// <summary>Raises the <see cref="HoveredBackgroundChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHoveredBackgroundChanged(SpannableControlPropertyChangedEventArgs<ISpannable?> args)
    {
        this.AllChildren[this.hoveredBackgroundChildIndex] = args.NewValue;
        this.HoveredBackgroundChanged?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ActiveBackgroundChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnActiveBackgroundChanged(SpannableControlPropertyChangedEventArgs<ISpannable?> args)
    {
        this.AllChildren[this.activeBackgroundChildIndex] = args.NewValue;
        this.ActiveBackgroundChanged?.Invoke(args);
    }

    /// <summary>Raises the <see cref="DisabledBackgroundChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDisabledBackgroundChanged(SpannableControlPropertyChangedEventArgs<ISpannable?> args)
    {
        this.AllChildren[this.disabledBackgroundChildIndex] = args.NewValue;
        this.DisabledBackgroundChanged?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ShowAnimationChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnShowAnimationChanged(
        SpannableControlPropertyChangedEventArgs<SpannableControlAnimator?> args) =>
        this.ShowAnimationChanged?.Invoke(args);

    /// <summary>Raises the <see cref="OnHideAnimationChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHideAnimationChanged(
        SpannableControlPropertyChangedEventArgs<SpannableControlAnimator?> args) =>
        this.HideAnimationChanged?.Invoke(args);

    /// <summary>Raises the <see cref="DisabledTextOpacityChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDisabledTextOpacityChanged(SpannableControlPropertyChangedEventArgs<float> args) =>
        this.DisabledTextOpacityChanged?.Invoke(args);

    /// <summary>Raises the <see cref="InterceptMouseWheelUpChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnInterceptMouseWheelUpChanged(SpannableControlPropertyChangedEventArgs<bool> args) =>
        this.InterceptMouseWheelUpChanged?.Invoke(args);

    /// <summary>Raises the <see cref="InterceptMouseWheelDownChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnInterceptMouseWheelDownChanged(SpannableControlPropertyChangedEventArgs<bool> args) =>
        this.InterceptMouseWheelDownChanged?.Invoke(args);

    /// <summary>Raises the <see cref="InterceptMouseWheelLeftChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnInterceptMouseWheelLeftChanged(SpannableControlPropertyChangedEventArgs<bool> args) =>
        this.InterceptMouseWheelLeftChanged?.Invoke(args);

    /// <summary>Raises the <see cref="InterceptMouseWheelRightChanged"/> event.</summary>
    /// <param name="args">A <see cref="SpannableControlPropertyChangedEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnInterceptMouseWheelRightChanged(SpannableControlPropertyChangedEventArgs<bool> args) =>
        this.InterceptMouseWheelRightChanged?.Invoke(args);
}

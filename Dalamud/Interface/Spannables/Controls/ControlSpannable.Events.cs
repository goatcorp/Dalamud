using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls;

/// <summary>A base spannable control that does nothing by itself.</summary>
public partial class ControlSpannable
{
    /// <summary>Occurs when the inside area needs to be drawn.</summary>
    public event SpannableDrawEventHandler? DrawInside;

    /// <summary>Occurs when the control is clicked.</summary>
    public event SpannableEventHandler? Click;

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

    /// <summary>Occurs when the property <see cref="ClipChildren"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? ClipChildrenChange;

    /// <summary>Occurs when the property <see cref="Text"/> is changing.</summary>
    public event PropertyChangeEventHandler<string?>? TextChange;

    /// <summary>Occurs when the property <see cref="TextStyle"/> is changing.</summary>
    public event PropertyChangeEventHandler<TextStyle>? TextStyleChange;

    /// <summary>Occurs when the property <see cref="Scale"/> is changing.</summary>
    public event PropertyChangeEventHandler<float>? ScaleChange;

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
    public event PropertyChangeEventHandler<ISpannableTemplate?>? NormalBackgroundChange;

    /// <summary>Occurs when the property <see cref="HoveredBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? HoveredBackgroundChange;

    /// <summary>Occurs when the property <see cref="ActiveBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? ActiveBackgroundChange;

    /// <summary>Occurs when the property <see cref="DisabledBackground"/> is changing.</summary>
    public event PropertyChangeEventHandler<ISpannableTemplate?>? DisabledBackgroundChange;

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

    /// <inheritdoc/>
    protected override void OnMouseClick(SpannableMouseEventArgs args)
    {
        base.OnMouseClick(args);
        if (!args.SuppressHandling && args.Step != SpannableEventStep.BeforeChildren)
            this.OnClick(args);
    }

    /// <inheritdoc/>
    protected override void OnMouseEnter(SpannableMouseEventArgs args)
    {
        base.OnMouseEnter(args);
        if (this.hoveredBackground is not null
            || this.activeBackground is not null
            || this.normalBackground is not null)
            this.RequestMeasure();
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave(SpannableMouseEventArgs args)
    {
        base.OnMouseLeave(args);
        if (this.hoveredBackground is not null
            || this.activeBackground is not null
            || this.normalBackground is not null)
            this.RequestMeasure();
    }

    /// <inheritdoc/>
    protected override void OnKeyPress(SpannableKeyPressEventArgs args)
    {
        base.OnKeyPress(args);
        if (!args.SuppressHandling && args.KeyChar is '\r' or '\n' && args.Step != SpannableEventStep.BeforeChildren)
            this.OnClick(args);
    }

    /// <summary>Raises the <see cref="DrawInside"/> event.</summary>
    /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDrawInside(SpannableDrawEventArgs args) => this.DrawInside?.Invoke(args);

    /// <summary>Raises the <see cref="Click"/> event.</summary>
    /// <param name="args">A <see cref="SpannableEventArgs"/> that contains the event data.</param>
    protected virtual void OnClick(SpannableEventArgs args) => this.Click?.Invoke(args);

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

    /// <inheritdoc/>
    protected override void OnVisibleChange(PropertyChangeEventArgs<bool> args)
    {
        if (args.State == PropertyChangeState.After)
            this.suppressNextMoveAnimation = true;
        base.OnVisibleChange(args);
    }

    /// <summary>Raises the <see cref="ClipChildrenChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnClipChildrenChange(PropertyChangeEventArgs<bool> args) =>
        this.ClipChildrenChange?.Invoke(args);

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
        }

        if (!this.suppressNextAnimation)
            this.transformationChangeAnimation?.Restart();
    }

    /// <summary>Raises the <see cref="NormalBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnNormalBackgroundChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.NormalBackgroundChange?.Invoke(args);

    /// <summary>Raises the <see cref="HoveredBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHoveredBackgroundChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.HoveredBackgroundChange?.Invoke(args);

    /// <summary>Raises the <see cref="ActiveBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnActiveBackgroundChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.ActiveBackgroundChange?.Invoke(args);

    /// <summary>Raises the <see cref="DisabledBackgroundChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnDisabledBackgroundChange(PropertyChangeEventArgs<ISpannableTemplate?> args) =>
        this.DisabledBackgroundChange?.Invoke(args);

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
}

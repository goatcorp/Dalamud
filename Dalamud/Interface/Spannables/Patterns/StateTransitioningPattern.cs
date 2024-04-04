using System.Numerics;

using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern spannable that draws different things depending on the displayed state.</summary>
public abstract class StateTransitioningPattern : AbstractPattern
{
    private readonly Spannable?[] spannables;
    private Spannable? backgroundSpannable;
    private Spannable? foregroundSpannable;
    private int previousState;
    private int state;
    private SpannableAnimator? hideAnimation;
    private SpannableAnimator? showAnimation;

    /// <summary>Initializes a new instance of the <see cref="StateTransitioningPattern"/> class.</summary>
    /// <param name="numStates">Number of possible states.</param>
    protected StateTransitioningPattern(int numStates) => this.spannables = new Spannable?[numStates];

    /// <summary>Occurs when the property <see cref="BackgroundSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? BackgroundSpannableChange;

    /// <summary>Occurs when the property <see cref="ForegroundSpannable"/> is changing.</summary>
    public event PropertyChangeEventHandler<Spannable?>? ForegroundSpannableChange;

    /// <summary>Occurs when the property <see cref="State"/> is changing.</summary>
    public event PropertyChangeEventHandler<int>? StateChange;

    /// <summary>Occurs when the property <see cref="HideAnimation"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableAnimator>? HideAnimationChange;

    /// <summary>Occurs when the property <see cref="ShowAnimation"/> is changing.</summary>
    public event PropertyChangeEventHandler<SpannableAnimator>? ShowAnimationChange;

    /// <summary>Gets or sets the constant background spannable.</summary>
    public Spannable? BackgroundSpannable
    {
        get => this.backgroundSpannable;
        set => this.HandlePropertyChange(
            nameof(this.BackgroundSpannable),
            ref this.backgroundSpannable,
            value,
            ReferenceEquals(this.backgroundSpannable, value),
            this.OnBackgroundSpannableChange);
    }

    /// <summary>Gets or sets the constant foreground spannable.</summary>
    public Spannable? ForegroundSpannable
    {
        get => this.foregroundSpannable;
        set => this.HandlePropertyChange(
            nameof(this.ForegroundSpannable),
            ref this.foregroundSpannable,
            value,
            ReferenceEquals(this.foregroundSpannable, value),
            this.OnForegroundSpannableChange);
    }

    /// <summary>Gets or sets the display state.</summary>
    public int State
    {
        get => this.state;
        set => this.HandlePropertyChange(
            nameof(this.State),
            ref this.state,
            value,
            this.state == value,
            this.OnStateChange);
    }

    /// <summary>Gets or sets the animation to play for the spannable that is disappearing on state change.</summary>
    public SpannableAnimator? HideAnimation
    {
        get => this.hideAnimation;
        set => this.HandlePropertyChange(
            nameof(this.HideAnimation),
            ref this.hideAnimation,
            value,
            ReferenceEquals(this.hideAnimation, value),
            this.OnHideAnimationChange);
    }

    /// <summary>Gets or sets the animation to play for the spannable that is disappearing on state change.</summary>
    public SpannableAnimator? ShowAnimation
    {
        get => this.showAnimation;
        set => this.HandlePropertyChange(
            nameof(this.ShowAnimation),
            ref this.showAnimation,
            value,
            ReferenceEquals(this.showAnimation, value),
            this.OnShowAnimationChange);
    }

    /// <summary>Gets the spannables.</summary>
    protected Span<Spannable?> Spannables => this.spannables.AsSpan();

    /// <inheritdoc/>
    protected override void OnMeasure(SpannableMeasureEventArgs args)
    {
        base.OnMeasure(args);

        var size = this.Boundary.RightBottom;
        this.backgroundSpannable?.RenderPassMeasure(size);

        for (var i = 0; i < this.spannables.Length; i++)
        {
            if (this.spannables[i] is not { } c)
                continue;

            if (this.state == i)
            {
                c.Visible = true;
                c.RenderPassMeasure(size);
                this.showAnimation?.Update(c);
            }
            else if (this.previousState == i)
            {
                if (this.hideAnimation?.IsRunning is not true)
                {
                    c.Visible = false;
                    c.RenderPassMeasure(size);
                }
                else
                {
                    c.Visible = true;
                    c.RenderPassMeasure(size);
                    this.hideAnimation.Update(c);
                }
            }
            else
            {
                c.Visible = false;
                c.RenderPassMeasure(size);
            }
        }

        this.foregroundSpannable?.RenderPassMeasure(size);

        if (this.showAnimation?.IsRunning is true || this.hideAnimation?.IsRunning is true)
            this.RequestMeasure();
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);
        this.backgroundSpannable?.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
        foreach (var c in this.spannables)
            c?.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
        this.foregroundSpannable?.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        this.backgroundSpannable?.RenderPassDraw(args.DrawListPtr);
        base.OnDrawInside(args);
        for (var i = 0; i < this.spannables.Length; i++)
        {
            if (this.spannables[i] is not { } c)
                continue;

            SpannableAnimator? animation = null;
            if (this.state == i)
                animation = this.showAnimation;
            else if (this.previousState == i)
                animation = this.hideAnimation;

            if (animation is null)
            {
                c.RenderPassDraw(args.DrawListPtr);
            }
            else
            {
                using var st = new ScopedTransformer(
                    args.DrawListPtr,
                    Matrix4x4.Multiply(animation.AnimatedTransformation, this.LocalTransformation),
                    Vector2.One,
                    animation.AnimatedOpacity);
                c.RenderPassDraw(args.DrawListPtr);
            }
        }

        this.foregroundSpannable?.RenderPassDraw(args.DrawListPtr);
    }

    /// <summary>Raises the <see cref="BackgroundSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnBackgroundSpannableChange(PropertyChangeEventArgs<Spannable?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            if (args.NewValue is not null)
                args.NewValue.ZOrder = int.MinValue;
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        }

        this.BackgroundSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ForegroundSpannableChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnForegroundSpannableChange(PropertyChangeEventArgs<Spannable?> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            if (args.NewValue is not null)
                args.NewValue.ZOrder = int.MaxValue;
            this.ReplaceChild(args.PreviousValue, args.NewValue);
        }

        this.ForegroundSpannableChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="StateChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnStateChange(PropertyChangeEventArgs<int> args)
    {
        if (args.State == PropertyChangeState.After)
        {
            this.previousState = args.PreviousValue;
            this.hideAnimation?.Restart();
            this.showAnimation?.Restart();
        }

        this.StateChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="HideAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnHideAnimationChange(PropertyChangeEventArgs<SpannableAnimator> args) =>
        this.HideAnimationChange?.Invoke(args);

    /// <summary>Raises the <see cref="ShowAnimationChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnShowAnimationChange(PropertyChangeEventArgs<SpannableAnimator> args) =>
        this.ShowAnimationChange?.Invoke(args);
}

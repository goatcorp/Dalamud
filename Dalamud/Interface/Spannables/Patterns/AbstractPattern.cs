using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;

namespace Dalamud.Interface.Spannables.Patterns;

#pragma warning disable SA1010

/// <summary>A spannable that can be used as a pattern, for backgrounds, borders, and alike.</summary>
public abstract class AbstractPattern : Spannable
{
    private Vector2 minSize = Vector2.Zero;
    private Vector2 size = new(float.PositiveInfinity);
    private Vector2 maxSize = new(float.PositiveInfinity);

    /// <summary>Occurs when the inside area needs to be drawn.</summary>
    public event SpannableDrawEventHandler? DrawInside;

    /// <summary>Occurs when the property <see cref="Size"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? SizeChange;

    /// <summary>Occurs when the property <see cref="MinSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? MinSizeChange;

    /// <summary>Occurs when the property <see cref="MaxSize"/> is changing.</summary>
    public event PropertyChangeEventHandler<Vector2>? MaxSizeChange;

    /// <summary>Gets or sets the size.</summary>
    /// <value><see cref="float.PositiveInfinity"/> for a dimension will use the size from the parent.</value>
    /// <remarks>This is not a hard limiting value.</remarks>
    public Vector2 Size
    {
        get => this.size;
        set => this.HandlePropertyChange(
            nameof(this.Size),
            ref this.size,
            value,
            this.size == value,
            this.OnSizeChange);
    }

    /// <summary>Gets or sets the minimum size.</summary>
    public Vector2 MinSize
    {
        get => this.minSize;
        set => this.HandlePropertyChange(
            nameof(this.MinSize),
            ref this.minSize,
            value,
            this.minSize == value,
            this.OnMinSizeChange);
    }

    /// <summary>Gets or sets the maximum size.</summary>
    public Vector2 MaxSize
    {
        get => this.maxSize;
        set => this.HandlePropertyChange(
            nameof(this.MaxSize),
            ref this.maxSize,
            value,
            this.maxSize == value,
            this.OnMaxSizeChange);
    }

    /// <inheritdoc/>
    protected override void OnMeasure(SpannableMeasureEventArgs args)
    {
        var s = this.Size;
        if (s.X >= float.PositiveInfinity)
            s.X = args.PreferredSize.X;
        if (s.Y >= float.PositiveInfinity)
            s.Y = args.PreferredSize.Y;

        s = Vector2.Clamp(s, this.MinSize, this.MaxSize);

        if (s.X >= float.PositiveInfinity || s.Y >= double.PositiveInfinity)
            s = Vector2.Zero;

        this.Boundary = new(Vector2.Zero, s);
        base.OnMeasure(args);
    }

    /// <inheritdoc/>
    protected override void OnDraw(SpannableDrawEventArgs args)
    {
        using var st = new ScopedTransformer(args.DrawListPtr, this.LocalTransformation, Vector2.One, 1f);
        var e = SpannableEventArgsPool.Rent<SpannableDrawEventArgs>();
        e.Initialize(this, SpannableEventStep.DirectTarget);
        e.InitializeDrawEvent(args.DrawListPtr);
        this.OnDrawInside(e);
        SpannableEventArgsPool.Return(e);
    }

    /// <summary>Raises the <see cref="DrawInside"/> event.</summary>
    /// <param name="args">A <see cref="SpannableDrawEventArgs"/> that contains the event data.</param>
    protected virtual void OnDrawInside(SpannableDrawEventArgs args) => this.DrawInside?.Invoke(args);

    /// <summary>Raises the <see cref="SizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnSizeChange(PropertyChangeEventArgs<Vector2> args) => this.SizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="MinSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMinSizeChange(PropertyChangeEventArgs<Vector2> args) => this.MinSizeChange?.Invoke(args);

    /// <summary>Raises the <see cref="MaxSizeChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnMaxSizeChange(PropertyChangeEventArgs<Vector2> args) => this.MaxSizeChange?.Invoke(args);
}

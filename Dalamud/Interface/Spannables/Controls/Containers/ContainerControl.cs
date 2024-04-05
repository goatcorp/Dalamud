using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;

using Dalamud.Interface.Animation;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Containers;

#pragma warning disable SA1010

/// <summary>A control that contains multiple spannables.</summary>
/// <remarks>Base container control implementation will place everything to top left.</remarks>
public class ContainerControl : ControlSpannable
{
    private Vector2 scroll;
    private RectVector4 scrollBoundary;
    private bool useDefaultScrollHandling;

    private Vector2 smoothScrollSource;
    private Vector2 smoothScrollTarget;
    private Easing? smoothScroll;

    /// <summary>Initializes a new instance of the <see cref="ContainerControl"/> class.</summary>
    public ContainerControl()
    {
        this.Children.CollectionChanged += this.ChildrenOnCollectionChanged;
        this.ClipChildren = true;
    }

    /// <summary>Occurs when the scroll position changes.</summary>
    public event PropertyChangeEventHandler<Vector2>? ScrollChange;

    /// <summary>Occurs when the scroll boundary changes.</summary>
    public event PropertyChangeEventHandler<RectVector4>? ScrollBoundaryChange;

    /// <summary>Occurs when the property <see cref="UseDefaultScrollHandling"/> is changing.</summary>
    public event PropertyChangeEventHandler<bool>? UseDefaultScrollHandlingChange;

    /// <summary>Gets or sets the current scroll distance.</summary>
    public Vector2 Scroll
    {
        get => this.scroll;
        set
        {
            var v = Vector2.Clamp(value, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);
            this.HandlePropertyChange(
                nameof(this.Scroll),
                ref this.scroll,
                v,
                this.scroll == v,
                this.OnScrollChange);
        }
    }

    /// <summary>Gets or sets the scroll boundary.</summary>
    public RectVector4 ScrollBoundary
    {
        get => this.scrollBoundary;
        set => this.HandlePropertyChange(
            nameof(this.ScrollBoundary),
            ref this.scrollBoundary,
            value,
            this.scrollBoundary == value,
            this.OnScrollBoundaryChange);
    }

    /// <summary>Gets or sets a value indicating whether to do perform the scroll handing.</summary>
    public bool UseDefaultScrollHandling
    {
        get => this.useDefaultScrollHandling;
        set => this.HandlePropertyChange(
            nameof(this.UseDefaultScrollHandling),
            ref this.useDefaultScrollHandling,
            value,
            this.useDefaultScrollHandling == value,
            this.OnUseDefaultScrollHandlingChange);
    }

    /// <summary>Gets the children.</summary>
    public ObservableCollection<Spannable> Children { get; } = [];

    /// <summary>Scrolls smoothly to the target.</summary>
    /// <param name="offset">The target offset.</param>
    /// <param name="easing">The easing.</param>
    public void SmoothScroll(Vector2 offset, Easing easing)
    {
        this.smoothScrollSource = this.scroll;
        this.smoothScrollTarget = Vector2.Clamp(offset, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);
        this.smoothScroll = easing;
        easing.Restart();
    }

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(Vector2 suggestedSize)
    {
        var unboundChildren = this.MeasureChildren(suggestedSize);

        var w = Math.Max(suggestedSize.X >= float.PositiveInfinity ? 0f : suggestedSize.X, unboundChildren.Right);
        var h = Math.Max(suggestedSize.Y >= float.PositiveInfinity ? 0f : suggestedSize.Y, unboundChildren.Bottom);

        var sx = suggestedSize.X < w ? w - suggestedSize.X : 0;
        var sy = suggestedSize.Y < h ? h - suggestedSize.Y : 0;
        w -= sx;
        h -= sy;
        if (this.UseDefaultScrollHandling)
            this.UpdateScrollBoundary(sx, sy);

        var newScroll = this.scroll;
        if (this.smoothScroll is not null)
        {
            this.smoothScroll.Update();
            if (this.smoothScroll.IsDone)
            {
                this.smoothScroll = null;
            }
            else
            {
                newScroll = Vector2.Lerp(
                    this.smoothScrollSource,
                    this.smoothScrollTarget,
                    (float)this.smoothScroll.Value);
            }
        }

        newScroll = Vector2.Clamp(newScroll, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);
        this.Scroll = newScroll;

        return new(Vector2.Zero, new(w, h));
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);
        this.PlaceChildren(args);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        base.OnDrawInside(args);
        this.DrawChildren(args);
    }

    /// <summary>Measures the children.</summary>
    /// <param name="suggestedSize">The suggested size of the content box of this container.</param>
    /// <returns>The measured content boundary.</returns>
    protected virtual RectVector4 MeasureChildren(Vector2 suggestedSize)
    {
        foreach (var childMeasurement in this.Children)
            childMeasurement.RenderPassMeasure(suggestedSize);

        var res = RectVector4.InvertedExtrema;
        foreach (var t in this.Children)
            res = RectVector4.Union(res, t.Boundary);
        return RectVector4.Normalize(res);
    }

    /// <summary>Updates <see cref="ScrollBoundary"/> from measured children.</summary>
    /// <param name="horizontal">The horizontal scrollable distance.</param>
    /// <param name="vertical">The vertical scrollable distance.</param>
    protected virtual void UpdateScrollBoundary(float horizontal, float vertical) =>
        this.ScrollBoundary = new(0, 0, horizontal, vertical);

    /// <summary>Updates transformation matrices for the children.</summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void PlaceChildren(SpannableEventArgs args)
    {
        var offset = (this.MeasuredContentBox.LeftTop - this.Scroll).Round(1 / this.EffectiveRenderScale);
        foreach (var cm in this.Children)
            cm.RenderPassPlace(Matrix4x4.CreateTranslation(new(offset, 0)), this.FullTransformation);
    }

    /// <summary>Draws the children.</summary>
    /// <param name="args">The event arguments.</param>
    protected virtual void DrawChildren(SpannableDrawEventArgs args)
    {
        foreach (var cm in this.Children)
            cm.RenderPassDraw(args.DrawListPtr);
    }

    /// <summary>Updates whether to intercept mouse wheel.</summary>
    /// <remarks>Called whenever <see cref="Scroll"/> or <see cref="ScrollBoundary"/> changes.</remarks>
    protected virtual void UpdateInterceptMouseWheel()
    {
        this.CaptureMouseOnMouseDown = this.scrollBoundary.LeftTop != this.scrollBoundary.RightBottom;
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(SpannableMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        if (args.SuppressHandling || !this.useDefaultScrollHandling)
            return;

        float scrollScale;
        if (this.Renderer.TryGetFontData(this.EffectiveRenderScale, this.TextStyle, out var fontData))
            scrollScale = fontData.ScaledFontSize;
        else
            scrollScale = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx * this.EffectiveRenderScale;

        this.Scroll -= args.WheelDelta * scrollScale * WindowsUiConfigHelper.GetWheelScrollLines();

        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="ScrollChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnScrollChange(PropertyChangeEventArgs<Vector2> args)
    {
        this.ScrollChange?.Invoke(args);

        if (args.State != PropertyChangeState.After)
            return;
        this.SuppressNextAnimation();
        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="ScrollBoundaryChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnScrollBoundaryChange(PropertyChangeEventArgs<RectVector4> args)
    {
        this.ScrollBoundaryChange?.Invoke(args);

        if (args.State != PropertyChangeState.After)
            return;
        this.Scroll = Vector2.Clamp(this.Scroll, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);
        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="UseDefaultScrollHandlingChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnUseDefaultScrollHandlingChange(PropertyChangeEventArgs<bool> args) =>
        this.UseDefaultScrollHandlingChange?.Invoke(args);

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } newItems:
                foreach (var item in newItems)
                    this.AddChild((Spannable)item);
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is { } oldItems:
                foreach (var item in oldItems)
                    this.RemoveChild((Spannable)item);
                break;
            case NotifyCollectionChangedAction.Replace when e.NewItems is { } newItems && e.OldItems is { } oldItems:
                for (var i = 0; i < newItems.Count; i++)
                    this.ReplaceChild((Spannable)oldItems[i], (Spannable)newItems[i]);
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                this.ClearChildren();
                foreach (var c in this.Children)
                    this.AddChild(c);
                break;
        }
    }
}

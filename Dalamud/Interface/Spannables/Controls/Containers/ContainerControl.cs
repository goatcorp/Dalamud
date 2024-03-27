using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Animation;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Utility.Enumeration;
using Dalamud.Utility.Numerics;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Spannables.Controls.Containers;

/// <summary>A control that contains multiple spannables.</summary>
/// <remarks>Base container control implementation will place everything to top left.</remarks>
public class ContainerControl : ControlSpannable
{
    private readonly List<ISpannableRenderPass?> childRenderPasses = new();
    private readonly ChildrenCollection childrenCollection;

    private Vector2 scroll;
    private RectVector4 scrollBoundary;
    private bool useDefaultScrollHandling = true;

    private Vector2 smoothScrollSource;
    private Vector2 smoothScrollTarget;
    private Easing? smoothScroll;

    /// <summary>Initializes a new instance of the <see cref="ContainerControl"/> class.</summary>
    public ContainerControl() => this.childrenCollection = new(this);

    /// <summary>Occurs when a child has been added.</summary>
    public event ControlChildEventHandler? ChildAdd;

    /// <summary>Occurs when a child has been changed.</summary>
    public event ControlChildEventHandler? ChildChange;

    /// <summary>Occurs when a child has been removed.</summary>
    public event ControlChildEventHandler? ChildRemove;

    /// <summary>Occurs when the scroll position changes.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, Vector2>? ScrollChange;

    /// <summary>Occurs when the scroll boundary changes.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, RectVector4>? ScrollBoundaryChange;

    /// <summary>Occurs when <see cref="UseDefaultScrollHandling"/> changes.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? UseDefaultScrollHandlingChange;

    /// <summary>Gets or sets the current scroll distance.</summary>
    public Vector2 Scroll
    {
        get => this.scroll;
        set => this.HandlePropertyChange(
            nameof(this.Scroll),
            ref this.scroll,
            Vector2.Clamp(value, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom),
            this.OnScrollChange);
    }

    /// <summary>Gets or sets the scroll boundary.</summary>
    public RectVector4 ScrollBoundary
    {
        get => this.scrollBoundary;
        set => this.HandlePropertyChange(
            nameof(this.ScrollBoundary),
            ref this.scrollBoundary,
            value,
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
            this.OnUseDefaultScrollHandlingChange);
    }

    /// <summary>Gets the children as an <see cref="IList{T}"/>.</summary>
    public IList<ISpannable> ChildrenList => this.childrenCollection;

    /// <summary>Gets the children as an <see cref="IReadOnlyList{T}"/>.</summary>
    public IReadOnlyList<ISpannable> ChildrenReadOnlyList => this.childrenCollection;

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
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args)
    {
        var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);

        for (var i = 0; i < children.Length; i++)
            renderPasses[i] ??= children[i].RentRenderPass(this.Renderer);

        var unboundChildren = this.MeasureChildren(args with { MaxSize = new(float.PositiveInfinity) }, renderPasses);

        var w = Math.Max(
            args.SuggestedSize.X >= float.PositiveInfinity ? 0f : args.SuggestedSize.X,
            unboundChildren.Right);
        var h = Math.Max(
            args.SuggestedSize.Y >= float.PositiveInfinity ? 0f : args.SuggestedSize.Y,
            unboundChildren.Bottom);

        var sx = args.SuggestedSize.X < w ? w - args.SuggestedSize.X : 0;
        var sy = args.SuggestedSize.Y < h ? h - args.SuggestedSize.Y : 0;
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
    protected override void OnCommitMeasurement(ControlCommitMeasurementEventArgs args)
    {
        base.OnCommitMeasurement(args);

        this.CommitMeasurementChildren(args, CollectionsMarshal.AsSpan(this.childRenderPasses));
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(
        ControlHandleInteractionEventArgs args,
        out SpannableLinkInteracted link)
    {
        base.OnHandleInteraction(args, out link);

        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        if (link.IsEmpty)
            this.HandleInteractionChildren(args, renderPasses, out link);
        else
            this.HandleInteractionChildren(args, renderPasses, out _);
    }

    /// <inheritdoc/>
    protected override void OnDraw(ControlDrawEventArgs args)
    {
        base.OnDraw(args);

        this.DrawChildren(args, CollectionsMarshal.AsSpan(this.childRenderPasses));
    }

    /// <summary>Measures the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="childrenPasses">The render passes for each of the children.</param>
    /// <returns>The measured content boundary.</returns>
    protected virtual RectVector4 MeasureChildren(
        SpannableMeasureArgs args,
        ReadOnlySpan<ISpannableRenderPass> childrenPasses)
    {
        for (var i = 0; i < childrenPasses.Length; i++)
        {
            var pass = childrenPasses[i];
            var innerId = this.InnerIdAvailableSlot + i;
            args.NotifyChild(
                pass,
                innerId,
                args with
                {
                    Scale = this.EffectiveScale,
                    MinSize = Vector2.Zero,
                    TextState = this.ActiveTextState.Fork(),
                });
        }

        var res = RectVector4.InvertedExtrema;
        foreach (var t in childrenPasses)
            res = RectVector4.Union(res, t.Boundary);
        return RectVector4.Normalize(res);
    }

    /// <summary>Updates <see cref="ScrollBoundary"/> from measured children.</summary>
    /// <param name="horizontal">The horizontal scrollable distance.</param>
    /// <param name="vertical">The vertical scrollable distance.</param>
    protected virtual void UpdateScrollBoundary(float horizontal, float vertical) =>
        this.ScrollBoundary = new(0, 0, horizontal, vertical);

    /// <summary>Commits measurements for the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="childPasses">The render passes for each of the children.</param>
    protected virtual void CommitMeasurementChildren(
        ControlCommitMeasurementEventArgs args,
        ReadOnlySpan<ISpannableRenderPass> childPasses)
    {
        var offset = (this.MeasuredContentBox.LeftTop - this.Scroll).Round(1 / this.EffectiveScale);
        foreach (var pass in childPasses)
            args.SpannableArgs.NotifyChild(pass, args.SpannableArgs, Matrix4x4.CreateTranslation(new(offset, 0)));
    }

    /// <summary>Handlers interactions for the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="childPasses">The render passes for each of the children.</param>
    /// <param name="link">The interacted link, if any.</param>
    protected virtual void HandleInteractionChildren(
        ControlHandleInteractionEventArgs args,
        ReadOnlySpan<ISpannableRenderPass> childPasses,
        out SpannableLinkInteracted link)
    {
        link = default;
        foreach (var pass in childPasses)
        {
            if (link.IsEmpty)
                args.SpannableArgs.NotifyChild(pass, args.SpannableArgs, out link);
            else
                args.SpannableArgs.NotifyChild(pass, args.SpannableArgs, out _);
        }
    }

    /// <summary>Draw the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="childPasses">Render passes for the children.</param>
    protected virtual void DrawChildren(ControlDrawEventArgs args, ReadOnlySpan<ISpannableRenderPass> childPasses)
    {
        foreach (var pass in childPasses)
            args.SpannableArgs.NotifyChild(pass, args.SpannableArgs);
    }

    /// <summary>Updates whether to intercept mouse wheel.</summary>
    /// <remarks>Called whenever <see cref="Scroll"/> or <see cref="ScrollBoundary"/> changes.</remarks>
    protected virtual void UpdateInterceptMouseWheel()
    {
        this.CaptureMouseWheel = this.scrollBoundary.LeftTop != this.scrollBoundary.RightBottom;
        this.CaptureMouseOnMouseDown = this.scrollBoundary.LeftTop != this.scrollBoundary.RightBottom;
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(ControlMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        if (args.Handled || !this.useDefaultScrollHandling)
            return;

        float scrollScale;
        if (this.Renderer.TryGetFontData(this.EffectiveScale, this.ActiveTextState.InitialStyle, out var fontData))
            scrollScale = fontData.ScaledFontSize;
        else
            scrollScale = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx * this.EffectiveScale;

        int nlines;
        unsafe
        {
            if (!SystemParametersInfoW(SPI.SPI_GETWHEELSCROLLLINES, 0, &nlines, 0))
                nlines = 3;
        }

        this.Scroll -= args.WheelDelta * scrollScale * nlines;

        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="ScrollChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnScrollChange(PropertyChangeEventArgs<ControlSpannable, Vector2> args)
    {
        this.ScrollChange?.Invoke(args);
        this.SuppressNextAnimation();
        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="ScrollBoundaryChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnScrollBoundaryChange(PropertyChangeEventArgs<ControlSpannable, RectVector4> args)
    {
        this.ScrollBoundaryChange?.Invoke(args);
        this.Scroll = Vector2.Clamp(this.Scroll, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);
        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="ChildAdd"/> event.</summary>
    /// <param name="args">A <see cref="ControlChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildAdd(ControlChildEventArgs args) => this.ChildAdd?.Invoke(args);

    /// <summary>Raises the <see cref="ChildChange"/> event.</summary>
    /// <param name="args">A <see cref="ControlChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildChange(ControlChildEventArgs args) => this.ChildChange?.Invoke(args);

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="ControlChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildRemove(ControlChildEventArgs args) => this.ChildRemove?.Invoke(args);

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnUseDefaultScrollHandlingChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.UseDefaultScrollHandlingChange?.Invoke(args);

    private class ChildrenCollection(ContainerControl owner)
        : IList<ISpannable>, IReadOnlyList<ISpannable>, ICollection
    {
        /// <inheritdoc cref="ICollection.Count"/>
        public int Count => owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot;

        /// <inheritdoc/>
        bool ICollection.IsSynchronized => false;

        /// <inheritdoc/>
        object ICollection.SyncRoot => this;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc cref="IList{T}.this"/>
        public ISpannable this[int index]
        {
            get
            {
                if (index < 0 || index >= owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot)
                    throw new IndexOutOfRangeException();
                return owner.AllSpannables[owner.AllSpannablesAvailableSlot + index]!;
            }

            set
            {
                if (index < 0 || index >= owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot)
                    throw new IndexOutOfRangeException();
                var prev = owner.AllSpannables[owner.AllSpannablesAvailableSlot + index];
                if (ReferenceEquals(prev, value))
                    return;

                owner.AllSpannables[owner.AllSpannablesAvailableSlot + index] =
                    value ?? throw new NullReferenceException();

                var e = SpannableControlEventArgsPool.Rent<ControlChildEventArgs>();
                e.Sender = owner;
                e.OldChild = prev;
                e.Child = value;
                e.Index = index;
                owner.OnChildChange(e);
                SpannableControlEventArgsPool.Return(e);
            }
        }

        /// <inheritdoc/>
        public void Add(ISpannable item)
        {
            owner.AllSpannables.Add(item ?? throw new NullReferenceException());
            owner.childRenderPasses.Add(null);

            var e = SpannableControlEventArgsPool.Rent<ControlChildEventArgs>();
            e.Sender = owner;
            e.Child = item;
            e.Index = owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot - 1;
            owner.OnChildAdd(e);
            SpannableControlEventArgsPool.Return(e);
        }

        /// <inheritdoc/>
        public void Clear() =>
            owner.AllSpannables.RemoveRange(
                owner.AllSpannablesAvailableSlot,
                owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot);

        /// <inheritdoc/>
        public bool Contains(ISpannable item) => owner.AllSpannables.IndexOf(item) >= owner.AllSpannablesAvailableSlot;

        /// <inheritdoc/>
        public void CopyTo(ISpannable[] array, int arrayIndex) =>
            CollectionsMarshal.AsSpan(owner.AllSpannables)[owner.AllSpannablesAvailableSlot..]
                              .CopyTo(array.AsSpan(arrayIndex));

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) => this.CopyTo((ISpannable[])array, index);

        /// <inheritdoc/>
        public bool Remove(ISpannable item)
        {
            var i = owner.AllSpannables.IndexOf(item);
            if (i < owner.AllSpannablesAvailableSlot)
                return false;

            this.RemoveAt(i);
            return true;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot)
                throw new IndexOutOfRangeException();

            var removedChild = owner.AllSpannables[owner.AllSpannablesAvailableSlot + index];
            removedChild!.ReturnRenderPass(owner.childRenderPasses[index]);

            owner.AllSpannables.RemoveAt(owner.AllSpannablesAvailableSlot + index);
            owner.childRenderPasses.RemoveAt(index);

            var e = SpannableControlEventArgsPool.Rent<ControlChildEventArgs>();
            e.Sender = owner;
            e.OldChild = removedChild;
            e.Child = removedChild;
            e.Index = index;
            owner.OnChildRemove(e);
            SpannableControlEventArgsPool.Return(e);
        }

        /// <inheritdoc/>
        public int IndexOf(ISpannable item)
        {
            var i = owner.AllSpannables.IndexOf(item);
            if (i >= owner.AllSpannablesAvailableSlot)
                return i - owner.AllSpannablesAvailableSlot;
            return -1;
        }

        /// <inheritdoc/>
        public void Insert(int index, ISpannable item)
        {
            if (index < 0 || index > owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot)
                throw new IndexOutOfRangeException();
            owner.AllSpannables.Insert(owner.AllSpannablesAvailableSlot + index, item);
            owner.childRenderPasses.Insert(index, null);

            var e = SpannableControlEventArgsPool.Rent<ControlChildEventArgs>();
            e.Sender = owner;
            e.Child = item;
            e.Index = index;
            owner.OnChildAdd(e);
            SpannableControlEventArgsPool.Return(e);
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public ListRangeEnumerator<ISpannable> GetEnumerator() =>
            new(owner.AllSpannables, owner.AllSpannablesAvailableSlot..);

        /// <inheritdoc/>
        IEnumerator<ISpannable> IEnumerable<ISpannable>.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
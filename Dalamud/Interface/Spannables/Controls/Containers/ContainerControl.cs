using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Animation;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Enumeration;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Containers;

/// <summary>A control that contains multiple spannables.</summary>
/// <remarks>Base container control implementation will place everything to top left.</remarks>
public class ContainerControl : ControlSpannable
{
    private readonly ChildrenCollection childrenCollection;

    private Vector2 scroll;
    private RectVector4 scrollBoundary;
    private bool useDefaultScrollHandling;

    private Vector2 smoothScrollSource;
    private Vector2 smoothScrollTarget;
    private Easing? smoothScroll;

    /// <summary>Initializes a new instance of the <see cref="ContainerControl"/> class.</summary>
    public ContainerControl() => this.childrenCollection = new(this);

    /// <summary>Occurs when a child has been added.</summary>
    public event ControlChildEventHandler? ChildAdd;

    /// <summary>Occurs when a child is changing.</summary>
    public event ControlChildEventHandler? ChildChange;

    /// <summary>Occurs when a child has been removed.</summary>
    public event ControlChildEventHandler? ChildRemove;

    /// <summary>Occurs when the scroll position changes.</summary>
    public event PropertyChangeEventHandler<Vector2>? ScrollChange;

    /// <summary>Occurs when the scroll boundary changes.</summary>
    public event PropertyChangeEventHandler<RectVector4>? ScrollBoundaryChange;

    /// <summary>Occurs when <see cref="UseDefaultScrollHandling"/> changes.</summary>
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

    /// <summary>Gets the children as an <see cref="IList{T}"/>.</summary>
    public IList<Spannable> ChildrenList => this.childrenCollection;

    /// <summary>Gets the children as an <see cref="IReadOnlyList{T}"/>.</summary>
    public IReadOnlyList<Spannable> ChildrenReadOnlyList => this.childrenCollection;

    private Span<Spannable> ChildMeasurements
    {
        get
        {
            var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];

            for (var i = 0; i < children.Length; i++)
            {
                children[i].Options.RenderScale = this.EffectiveRenderScale;
                children[i].Renderer = this.Renderer;
                children[i].ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.InnerIdAvailableSlot + i);
            }

            return children;
        }
    }

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
        var unboundChildren = this.MeasureChildren(suggestedSize, this.ChildMeasurements);

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
        this.PlaceChildren(args, this.ChildMeasurements);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        base.OnDrawInside(args);
        this.DrawChildren(args, this.ChildMeasurements);
    }

    /// <summary>Measures the children.</summary>
    /// <param name="suggestedSize">The suggested size of the content box of this container.</param>
    /// <param name="children">The render passes for each of the children.</param>
    /// <returns>The measured content boundary.</returns>
    protected virtual RectVector4 MeasureChildren(
        Vector2 suggestedSize,
        ReadOnlySpan<Spannable> children)
    {
        foreach (var childMeasurement in children)
        {
            childMeasurement.Options.PreferredSize = suggestedSize;
            childMeasurement.Options.VisibleSize = this.Options.VisibleSize;
            childMeasurement.RenderPassMeasure();
        }

        var res = RectVector4.InvertedExtrema;
        foreach (var t in children)
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
    /// <param name="children">The render passes for each of the children.</param>
    protected virtual void PlaceChildren(
        SpannableEventArgs args,
        ReadOnlySpan<Spannable> children)
    {
        var offset = (this.MeasuredContentBox.LeftTop - this.Scroll).Round(1 / this.EffectiveRenderScale);
        foreach (var cm in children)
            cm.RenderPassPlace(Matrix4x4.CreateTranslation(new(offset, 0)), this.FullTransformation);
    }

    /// <summary>Draws the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">Child measurements.</param>
    protected virtual void DrawChildren(
        SpannableDrawEventArgs args,
        ReadOnlySpan<Spannable> children)
    {
        foreach (var cm in children)
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
        if (args.SuppressHandling || !this.useDefaultScrollHandling || this.Renderer is null)
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

    /// <summary>Raises the <see cref="ChildAdd"/> event.</summary>
    /// <param name="args">A <see cref="SpannableChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildAdd(SpannableChildEventArgs args)
    {
        args.Child.PropertyChange += this.ChildOnPropertyChange;
        this.ChildAdd?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ChildChange"/> event.</summary>
    /// <param name="args">A <see cref="SpannableChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildChange(SpannableChildEventArgs args)
    {
        if (args.OldChild is { } oldChild)
            oldChild.PropertyChange -= this.ChildOnPropertyChange;
        args.Child.PropertyChange += this.ChildOnPropertyChange;
        this.ChildChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="SpannableChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildRemove(SpannableChildEventArgs args)
    {
        args.Child.PropertyChange -= this.ChildOnPropertyChange;
        this.ChildRemove?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnUseDefaultScrollHandlingChange(PropertyChangeEventArgs<bool> args) =>
        this.UseDefaultScrollHandlingChange?.Invoke(args);

    private void ChildOnPropertyChange(PropertyChangeEventArgs args) => this.RequestMeasure();

    private class ChildrenCollection(ContainerControl owner)
        : IList<Spannable>, IReadOnlyList<Spannable>, ICollection
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
        public Spannable this[int index]
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

                var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
                e.Initialize(owner, SpannableEventStep.DirectTarget);
                e.InitializeChildProperties(index, value, prev);
                owner.OnChildChange(e);
                SpannableEventArgsPool.Return(e);
            }
        }

        /// <inheritdoc/>
        public void Add(Spannable item)
        {
            owner.AllSpannables.Add(item ?? throw new NullReferenceException());

            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
            e.Initialize(owner, SpannableEventStep.DirectTarget);
            e.InitializeChildProperties(owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot - 1, item, null);
            owner.OnChildAdd(e);
            SpannableEventArgsPool.Return(e);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();

            while (owner.AllSpannables.Count > owner.AllSpannablesAvailableSlot)
            {
                var i = owner.AllSpannables.Count - 1;
                e.Initialize(owner, SpannableEventStep.DirectTarget);
                e.InitializeChildProperties(i, owner.AllSpannables[i]!, owner.AllSpannables[i]);
                owner.OnChildRemove(e);
                owner.AllSpannables.RemoveAt(i);
            }

            SpannableEventArgsPool.Return(e);
        }

        /// <inheritdoc/>
        public bool Contains(Spannable item) => owner.AllSpannables.IndexOf(item) >= owner.AllSpannablesAvailableSlot;

        /// <inheritdoc/>
        public void CopyTo(Spannable[] array, int arrayIndex) =>
            CollectionsMarshal.AsSpan(owner.AllSpannables)[owner.AllSpannablesAvailableSlot..]
                              .CopyTo(array.AsSpan(arrayIndex));

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) => this.CopyTo((Spannable[])array, index);

        /// <inheritdoc/>
        public bool Remove(Spannable item)
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

            owner.AllSpannables.RemoveAt(owner.AllSpannablesAvailableSlot + index);

            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
            e.Initialize(owner, SpannableEventStep.DirectTarget);
            e.InitializeChildProperties(index, removedChild!, removedChild);
            owner.OnChildRemove(e);
            SpannableEventArgsPool.Return(e);
        }

        /// <inheritdoc/>
        public int IndexOf(Spannable item)
        {
            var i = owner.AllSpannables.IndexOf(item);
            if (i >= owner.AllSpannablesAvailableSlot)
                return i - owner.AllSpannablesAvailableSlot;
            return -1;
        }

        /// <inheritdoc/>
        public void Insert(int index, Spannable item)
        {
            if (index < 0 || index > owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot)
                throw new IndexOutOfRangeException();
            owner.AllSpannables.Insert(owner.AllSpannablesAvailableSlot + index, item);

            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
            e.Initialize(owner, SpannableEventStep.DirectTarget);
            e.InitializeChildProperties(index, item, null);
            owner.OnChildAdd(e);
            SpannableEventArgsPool.Return(e);
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public ListRangeEnumerator<Spannable> GetEnumerator() =>
            new(owner.AllSpannables, owner.AllSpannablesAvailableSlot..);

        /// <inheritdoc/>
        IEnumerator<Spannable> IEnumerable<Spannable>.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}

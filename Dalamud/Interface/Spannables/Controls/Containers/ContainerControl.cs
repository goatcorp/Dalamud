using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Utility.Enumeration;
using Dalamud.Utility.Numerics;

using ImGuiNET;

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
    private bool useAutoScrollBoundary = true;

    /// <summary>Initializes a new instance of the <see cref="ContainerControl"/> class.</summary>
    public ContainerControl() => this.childrenCollection = new ChildrenCollection(this);

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

    /// <summary>Occurs when <see cref="UseAutoScrollBoundary"/> changes.</summary>
    public event PropertyChangeEventHandler<ControlSpannable, bool>? UseAutoScrollBoundaryChange;

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

    /// <summary>Gets or sets a value indicating whether to adjust <see cref="ScrollBoundary"/> automatically according
    /// to children measurements.</summary>
    public bool UseAutoScrollBoundary
    {
        get => this.useAutoScrollBoundary;
        set => this.HandlePropertyChange(
            nameof(this.UseAutoScrollBoundary),
            ref this.useAutoScrollBoundary,
            value,
            this.OnUseAutoScrollBoundaryChange);
    }

    /// <summary>Gets the children as an <see cref="IList{T}"/>.</summary>
    public IList<ISpannable> ChildrenList => this.childrenCollection;

    /// <summary>Gets the children as an <see cref="IReadOnlyList{T}"/>.</summary>
    public IReadOnlyList<ISpannable> ChildrenReadOnlyList => this.childrenCollection;

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args)
    {
        var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);

        for (var i = 0; i < children.Length; i++)
            renderPasses[i] ??= children[i].RentRenderPass(args.RenderPass.Renderer);

        var measureChildArgs = args;

        // If either of minimum size for domensions are instructed to wrap contents, try measuring without bounds.
        if (this.IsMinWidthWrapContent)
        {
            measureChildArgs.MinSize.X =
                measureChildArgs.MaxSize.X = measureChildArgs.SuggestedSize.X = float.PositiveInfinity;
        }

        if (this.IsMinHeightWrapContent)
        {
            measureChildArgs.MinSize.Y =
                measureChildArgs.MaxSize.Y = measureChildArgs.SuggestedSize.Y = float.PositiveInfinity;
        }

        var unboundChildren = this.MeasureChildren(measureChildArgs, children, renderPasses);

        // Now that it's measured without bounds, if it fits in the given max size, measure again so that it can
        // utilize the rest of the area.
        if (this.IsMinWidthWrapContent || this.IsMinHeightWrapContent)
        {
            measureChildArgs = args;

            var dirty = 2;
            if (unboundChildren.Width > args.MaxSize.X)
            {
                measureChildArgs.MinSize.X =
                    measureChildArgs.MaxSize.X = measureChildArgs.SuggestedSize.X = float.PositiveInfinity;
                dirty--;
            }

            if (unboundChildren.Height > args.MaxSize.Y)
            {
                measureChildArgs.MinSize.Y =
                    measureChildArgs.MaxSize.Y = measureChildArgs.SuggestedSize.Y = float.PositiveInfinity;
                dirty--;
            }
            
            if (dirty != 0)
                unboundChildren = this.MeasureChildren(measureChildArgs, children, renderPasses);
        }

        var boundChildren = Vector2.Min(unboundChildren.Size, args.MaxSize);

        if (this.UseAutoScrollBoundary)
        {
            this.UpdateScrollBoundary(
                unboundChildren.Width - boundChildren.X,
                unboundChildren.Height - boundChildren.Y);
        }

        this.Scroll = Vector2.Clamp(this.scroll, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);

        return unboundChildren;
    }

    /// <inheritdoc/>
    protected override void OnCommitMeasurement(ControlCommitMeasurementEventArgs args)
    {
        base.OnCommitMeasurement(args);

        var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        this.CommitMeasurementChildren(args, children, renderPasses);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(
        ControlHandleInteractionEventArgs args,
        out SpannableLinkInteracted link)
    {
        base.OnHandleInteraction(args, out link);

        var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        if (link.IsEmpty)
            this.HandleInteractionChildren(args, children, renderPasses, out link);
        else
            this.HandleInteractionChildren(args, children, renderPasses, out _);
    }

    /// <inheritdoc/>
    protected override void OnDraw(ControlDrawEventArgs args)
    {
        base.OnDraw(args);

        var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        this.DrawChildren(args, children, renderPasses);
    }

    /// <summary>Measures the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    /// <returns>The measured content boundary.</returns>
    protected virtual RectVector4 MeasureChildren(
        SpannableMeasureArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            var innerId = this.InnerIdAvailableSlot + i;
            args.NotifyChild(child, pass, innerId, Vector2.Zero, args.MaxSize, this.ActiveTextState);
        }

        var res = RectVector4.InvertedExtrema;
        foreach (var t in renderPasses)
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
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    protected virtual void CommitMeasurementChildren(
        ControlCommitMeasurementEventArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        var offset = this.MeasuredContentBox.LeftTop - this.Scroll;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            args.SpannableArgs.NotifyChild(child, pass, offset, Matrix4x4.Identity);
        }
    }

    /// <summary>Handlers interactions for the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    /// <param name="link">The interacted link, if any.</param>
    protected virtual void HandleInteractionChildren(
        ControlHandleInteractionEventArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses,
        out SpannableLinkInteracted link)
    {
        link = default;
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            if (link.IsEmpty)
                args.SpannableArgs.NotifyChild(child, pass, out link);
            else
                args.SpannableArgs.NotifyChild(child, pass, out _);
        }
    }

    /// <summary>Draw the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    protected virtual void DrawChildren(
        ControlDrawEventArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        for (var i = 0; i < children.Length; i++)
        {
            var child = children[i];
            var pass = renderPasses[i];
            args.SpannableArgs.NotifyChild(child, pass);
        }
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
        float scrollScale;
        if (this.Renderer.TryGetFontData(this.Scale, this.ActiveTextState.InitialStyle, out var fontData))
            scrollScale = fontData.ScaledFontSize;
        else
            scrollScale = Service<FontAtlasFactory>.Get().DefaultFontSpec.SizePx * this.Scale;

        int nlines;
        unsafe
        {
            if (!SystemParametersInfoW(SPI.SPI_GETWHEELSCROLLLINES, 0, &nlines, 0))
                nlines = 3;
        }

        this.Scroll -= args.WheelDelta * scrollScale * nlines;
        this.SuppressNextMoveAnimation();

        this.UpdateInterceptMouseWheel();
    }

    /// <summary>Raises the <see cref="ScrollChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnScrollChange(PropertyChangeEventArgs<ControlSpannable, Vector2> args)
    {
        this.ScrollChange?.Invoke(args);
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
    protected virtual void OnUseAutoScrollBoundaryChange(PropertyChangeEventArgs<ControlSpannable, bool> args) =>
        this.UseAutoScrollBoundaryChange?.Invoke(args);

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

                var e = ControlEventArgsPool.Rent<ControlChildEventArgs>();
                e.Sender = owner;
                e.OldChild = prev;
                e.Child = value;
                e.Index = index;
                owner.OnChildChange(e);
                ControlEventArgsPool.Return(e);
            }
        }

        /// <inheritdoc/>
        public void Add(ISpannable item)
        {
            owner.AllSpannables.Add(item ?? throw new NullReferenceException());
            owner.childRenderPasses.Add(null);

            var e = ControlEventArgsPool.Rent<ControlChildEventArgs>();
            e.Sender = owner;
            e.Child = item;
            e.Index = owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot - 1;
            owner.OnChildAdd(e);
            ControlEventArgsPool.Return(e);
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

            var e = ControlEventArgsPool.Rent<ControlChildEventArgs>();
            e.Sender = owner;
            e.OldChild = removedChild;
            e.Child = removedChild;
            e.Index = index;
            owner.OnChildRemove(e);
            ControlEventArgsPool.Return(e);
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

            var e = ControlEventArgsPool.Rent<ControlChildEventArgs>();
            e.Sender = owner;
            e.Child = item;
            e.Index = index;
            owner.OnChildAdd(e);
            ControlEventArgsPool.Return(e);
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

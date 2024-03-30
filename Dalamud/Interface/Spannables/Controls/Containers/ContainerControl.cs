using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Animation;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Utility.Enumeration;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Containers;

/// <summary>A control that contains multiple spannables.</summary>
/// <remarks>Base container control implementation will place everything to top left.</remarks>
public class ContainerControl : ControlSpannable
{
    private readonly List<ISpannableMeasurement?> childMeasurementsList = new();
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

    private Span<ISpannableMeasurement> ChildMeasurements
    {
        get
        {
            var children = CollectionsMarshal.AsSpan(this.AllSpannables)[this.AllSpannablesAvailableSlot..];
            var childMeasurements = CollectionsMarshal.AsSpan(this.childMeasurementsList);

            for (var i = 0; i < children.Length; i++)
            {
                childMeasurements[i] ??= children[i].RentMeasurement(this.Renderer);
                childMeasurements[i].RenderScale = this.EffectiveRenderScale;
                childMeasurements[i].ImGuiGlobalId = this.GetGlobalIdFromInnerId(this.InnerIdAvailableSlot + i);
            }

            return childMeasurements;
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
    public override ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
    {
        foreach (var m in this.childMeasurementsList)
        {
            if (m is null)
                continue;
            if (m.Boundary.Contains(m.PointToClient(screenOffset)))
                return m;
        }
        
        return base.FindChildMeasurementAt(screenOffset);
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
    protected override void OnUpdateTransformation(SpannableEventArgs args)
    {
        base.OnUpdateTransformation(args);
        this.UpdateTransformationChildren(args, this.ChildMeasurements);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(SpannableEventArgs args)
    {
        base.OnHandleInteraction(args);
        this.HandleInteractionChildren(args, this.ChildMeasurements);
    }

    /// <inheritdoc/>
    protected override void OnDraw(SpannableDrawEventArgs args)
    {
        base.OnDraw(args);
        this.DrawChildren(args, this.ChildMeasurements);
    }

    /// <summary>Measures the children.</summary>
    /// <param name="suggestedSize">The suggested size of the content box of this container.</param>
    /// <param name="childMeasurements">The render passes for each of the children.</param>
    /// <returns>The measured content boundary.</returns>
    protected virtual RectVector4 MeasureChildren(
        Vector2 suggestedSize,
        ReadOnlySpan<ISpannableMeasurement> childMeasurements)
    {
        foreach (var childMeasurement in childMeasurements)
        {
            childMeasurement.Options.Size = suggestedSize;
            childMeasurement.Options.VisibleSize = this.MeasurementOptions.VisibleSize;
            childMeasurement.Measure();
        }

        var res = RectVector4.InvertedExtrema;
        foreach (var t in childMeasurements)
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
    /// <param name="childMeasurements">The render passes for each of the children.</param>
    protected virtual void UpdateTransformationChildren(
        SpannableEventArgs args,
        ReadOnlySpan<ISpannableMeasurement> childMeasurements)
    {
        var offset = (this.MeasuredContentBox.LeftTop - this.Scroll).Round(1 / this.EffectiveRenderScale);
        foreach (var cm in childMeasurements)
            cm.UpdateTransformation(Matrix4x4.CreateTranslation(new(offset, 0)), this.FullTransformation);
    }
    
    /// <summary>Handlers interactions for the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="childMeasurements">Child measurements.</param>
    protected virtual void HandleInteractionChildren(
        SpannableEventArgs args,
        ReadOnlySpan<ISpannableMeasurement> childMeasurements)
    {
        foreach (var cm in childMeasurements)
            cm.HandleInteraction();
    }

    /// <summary>Draws the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="childMeasurements">Child measurements.</param>
    protected virtual void DrawChildren(
        SpannableDrawEventArgs args,
        ReadOnlySpan<ISpannableMeasurement> childMeasurements)
    {
        foreach (var cm in childMeasurements)
            cm.Draw(args.DrawListPtr);
    }

    /// <summary>Updates whether to intercept mouse wheel.</summary>
    /// <remarks>Called whenever <see cref="Scroll"/> or <see cref="ScrollBoundary"/> changes.</remarks>
    protected virtual void UpdateInterceptMouseWheel()
    {
        this.CaptureMouseWheel = this.scrollBoundary.LeftTop != this.scrollBoundary.RightBottom;
        this.CaptureMouseOnMouseDown = this.scrollBoundary.LeftTop != this.scrollBoundary.RightBottom;
    }

    /// <inheritdoc/>
    protected override void OnMouseWheel(SpannableMouseEventArgs args)
    {
        base.OnMouseWheel(args);
        if (args.Handled || !this.useDefaultScrollHandling)
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
        args.Child.SpannableChange += this.ChildOnSpannableChange;
        this.ChildAdd?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ChildChange"/> event.</summary>
    /// <param name="args">A <see cref="SpannableChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildChange(SpannableChildEventArgs args)
    {
        if (args.OldChild is { } oldChild)
            oldChild.SpannableChange -= this.ChildOnSpannableChange;
        args.Child.SpannableChange += this.ChildOnSpannableChange;
        this.ChildChange?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="SpannableChildEventArgs"/> that contains the event data.</param>
    protected virtual void OnChildRemove(SpannableChildEventArgs args)
    {
        args.Child.SpannableChange -= this.ChildOnSpannableChange;
        this.ChildRemove?.Invoke(args);
    }

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnUseDefaultScrollHandlingChange(PropertyChangeEventArgs<bool> args) =>
        this.UseDefaultScrollHandlingChange?.Invoke(args);

    private void ChildOnSpannableChange(ISpannable obj) => this.OnSpannableChange(this);

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

                var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
                e.Sender = owner;
                e.OldChild = prev;
                e.Child = value;
                e.Index = index;
                owner.OnChildChange(e);
                SpannableEventArgsPool.Return(e);
            }
        }

        /// <inheritdoc/>
        public void Add(ISpannable item)
        {
            owner.AllSpannables.Add(item ?? throw new NullReferenceException());
            owner.childMeasurementsList.Add(null);

            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
            e.Sender = owner;
            e.Child = item;
            e.Index = owner.AllSpannables.Count - owner.AllSpannablesAvailableSlot - 1;
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
                e.Sender = owner;
                e.OldChild = e.Child = owner.AllSpannables[i]!;
                e.Index = i;
                owner.OnChildRemove(e);
                owner.AllSpannables.RemoveAt(i);
            }

            SpannableEventArgsPool.Return(e);
        }

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
            removedChild!.ReturnMeasurement(owner.childMeasurementsList[index]);

            owner.AllSpannables.RemoveAt(owner.AllSpannablesAvailableSlot + index);
            owner.childMeasurementsList.RemoveAt(index);

            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
            e.Sender = owner;
            e.OldChild = removedChild;
            e.Child = removedChild;
            e.Index = index;
            owner.OnChildRemove(e);
            SpannableEventArgsPool.Return(e);
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
            owner.childMeasurementsList.Insert(index, null);

            var e = SpannableEventArgsPool.Rent<SpannableChildEventArgs>();
            e.Sender = owner;
            e.Child = item;
            e.Index = index;
            owner.OnChildAdd(e);
            SpannableEventArgsPool.Return(e);
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

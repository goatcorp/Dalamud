using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Controls.EventHandlerDelegates;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Utility.Enumeration;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.Containers;

/// <summary>A control that contains multiple spannables.</summary>
/// <remarks>Base container control implementation will place everything to top left.</remarks>
public class ContainerControl
    : ControlSpannable, IList<ISpannable>, IReadOnlyList<ISpannable>, ICollection
{
    private readonly List<ISpannableRenderPass?> childRenderPasses = new();

    private Vector2 scroll;
    private RectVector4 scrollBoundary;

    /// <summary>Occurs when a child has been added.</summary>
    public event ControlChildEventHandler? ChildAdd;

    /// <summary>Occurs when a child has been changed.</summary>
    public event ControlChildEventHandler? ChildChange;

    /// <summary>Occurs when a child has been removed.</summary>
    public event ControlChildEventHandler? ChildRemove;

    /// <summary>Occurs when the current scroll position changes.</summary>
    public event PropertyChangedEventHandler<ControlSpannable, Vector2>? ScrollChanged;

    /// <summary>Occurs when the maximum scroll region changes.</summary>
    public event PropertyChangedEventHandler<ControlSpannable, RectVector4>? ScrollBoundaryChanged;

    /// <summary>Gets or sets the current scroll distance.</summary>
    public Vector2 Scroll
    {
        get => this.scroll;
        set => this.HandlePropertyChange(
            nameof(this.Scroll),
            ref this.scroll,
            value,
            this.OnScrollChanged);
    }

    /// <summary>Gets or sets the scroll boundary.</summary>
    public RectVector4 ScrollBoundary
    {
        get => this.scrollBoundary;
        set => this.HandlePropertyChange(
            nameof(this.ScrollBoundary),
            ref this.scrollBoundary,
            value,
            this.OnScrollBoundaryChanged);
    }

    /// <summary>Gets or sets a value indicating whether to adjust <see cref="ScrollBoundary"/> automatically according
    /// to children measurements.</summary>
    public bool UseAutoScrollBoundary { get; set; } = true;

    /// <inheritdoc cref="ICollection.Count"/>
    public int Count => this.AllChildren.Count - this.AllChildrenAvailableSlot;

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
            if (index < 0 || index >= this.AllChildren.Count - this.AllChildrenAvailableSlot)
                throw new IndexOutOfRangeException();
            return this.AllChildren[this.AllChildrenAvailableSlot + index]!;
        }

        set
        {
            if (index < 0 || index >= this.AllChildren.Count - this.AllChildrenAvailableSlot)
                throw new IndexOutOfRangeException();
            var prev = this.AllChildren[this.AllChildrenAvailableSlot + index];
            this.AllChildren[this.AllChildrenAvailableSlot + index] = value ?? throw new NullReferenceException();
            this.OnChildChange(new() { Sender = this, OldChild = prev, Child = value, Index = index });
        }
    }

    /// <inheritdoc/>
    public void Add(ISpannable item)
    {
        this.AllChildren.Add(item ?? throw new NullReferenceException());
        this.childRenderPasses.Add(null);
        this.OnChildAdd(
            new()
            {
                Sender = this,
                Child = item,
                Index = this.AllChildren.Count - this.AllChildrenAvailableSlot - 1,
            });
    }

    /// <inheritdoc/>
    public void Clear() =>
        this.AllChildren.RemoveRange(
            this.AllChildrenAvailableSlot,
            this.AllChildren.Count - this.AllChildrenAvailableSlot);

    /// <inheritdoc/>
    public bool Contains(ISpannable item) => this.AllChildren.IndexOf(item) >= this.AllChildrenAvailableSlot;

    /// <inheritdoc/>
    public void CopyTo(ISpannable[] array, int arrayIndex) =>
        CollectionsMarshal.AsSpan(this.AllChildren)[this.AllChildrenAvailableSlot..].CopyTo(array.AsSpan(arrayIndex));

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index) => this.CopyTo((ISpannable[])array, index);

    /// <inheritdoc/>
    public bool Remove(ISpannable item)
    {
        var i = this.AllChildren.IndexOf(item);
        if (i < this.AllChildrenAvailableSlot)
            return false;

        this.RemoveAt(i);
        return true;
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= this.AllChildren.Count - this.AllChildrenAvailableSlot)
            throw new IndexOutOfRangeException();

        var removedChild = this.AllChildren[this.AllChildrenAvailableSlot + index];
        removedChild!.ReturnRenderPass(this.childRenderPasses[index]);

        this.AllChildren.RemoveAt(this.AllChildrenAvailableSlot + index);
        this.childRenderPasses.RemoveAt(index);
        this.OnChildRemove(new() { Sender = this, Child = removedChild!, Index = index });
    }

    /// <inheritdoc/>
    public int IndexOf(ISpannable item)
    {
        var i = this.AllChildren.IndexOf(item);
        if (i >= this.AllChildrenAvailableSlot)
            return i - this.AllChildrenAvailableSlot;
        return -1;
    }

    /// <inheritdoc/>
    public void Insert(int index, ISpannable item)
    {
        if (index < 0 || index > this.AllChildren.Count - this.AllChildrenAvailableSlot)
            throw new IndexOutOfRangeException();
        this.AllChildren.Insert(this.AllChildrenAvailableSlot + index, item);
        this.childRenderPasses.Insert(index, null);
        this.OnChildAdd(
            new()
            {
                Sender = this,
                Child = item,
                Index = index,
            });
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public ListRangeEnumerator<ISpannable> GetEnumerator() => new(this.AllChildren, this.AllChildrenAvailableSlot..);

    /// <inheritdoc/>
    IEnumerator<ISpannable> IEnumerable<ISpannable>.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    protected override RectVector4 MeasureContentBox(SpannableMeasureArgs args, in RectVector4 availableContentBox)
    {
        var children = CollectionsMarshal.AsSpan(this.AllChildren)[this.AllChildrenAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        this.MeasureChildren(args, children, renderPasses, availableContentBox);

        var res = RectVector4.InvertedExtrema;
        foreach (var t in renderPasses)
            res = RectVector4.Union(res, t.Boundary);

        if (this.UseAutoScrollBoundary)
        {
            this.UpdateScrollBoundary(
                Math.Max(0, res.Right - this.MeasuredInteractiveBox.Width),
                Math.Max(0, res.Bottom - this.MeasuredInteractiveBox.Height));
        }

        this.Scroll = Vector2.Clamp(this.scroll, this.scrollBoundary.LeftTop, this.scrollBoundary.RightBottom);

        return res;
    }

    /// <inheritdoc/>
    protected override void OnCommitMeasurement(ControlCommitMeasurementArgs args)
    {
        base.OnCommitMeasurement(args);

        var children = CollectionsMarshal.AsSpan(this.AllChildren)[this.AllChildrenAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        this.CommitMeasurementChildren(args, children, renderPasses);
    }

    /// <inheritdoc/>
    protected override void OnHandleInteraction(
        ControlHandleInteractionArgs args,
        out SpannableLinkInteracted link)
    {
        base.OnHandleInteraction(args, out link);

        var children = CollectionsMarshal.AsSpan(this.AllChildren)[this.AllChildrenAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        if (link.IsEmpty)
            this.HandleInteractionChildren(args, children, renderPasses, out link);
        else
            this.HandleInteractionChildren(args, children, renderPasses, out _);
    }

    /// <inheritdoc/>
    protected override void OnDraw(ControlDrawArgs args)
    {
        base.OnDraw(args);

        var children = CollectionsMarshal.AsSpan(this.AllChildren)[this.AllChildrenAvailableSlot..];
        var renderPasses = CollectionsMarshal.AsSpan(this.childRenderPasses);
        this.DrawChildren(args, children, renderPasses);
    }

    /// <summary>Measures the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    /// <param name="availableContentBox">The available content box.</param>
    protected virtual void MeasureChildren(
        SpannableMeasureArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses,
        in RectVector4 availableContentBox)
    {
        for (var i = 0; i < children.Length; i++)
        {
            renderPasses[i].MeasureSpannable(
                new(
                    children[i],
                    renderPasses[i],
                    availableContentBox.Size,
                    this.Scale,
                    this.TextState with { InitialStyle = this.TextState.LastStyle }));
        }
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
        ControlCommitMeasurementArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        for (var i = 0; i < children.Length; i++)
        {
            args.MeasureArgs.NotifyChild(
                children[i],
                renderPasses[i],
                this.MeasuredContentBox.LeftTop,
                Matrix4x4.Identity);
        }
    }

    /// <summary>Handlers interactions for the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    /// <param name="link">The interacted link, if any.</param>
    protected virtual void HandleInteractionChildren(
        ControlHandleInteractionArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses,
        out SpannableLinkInteracted link)
    {
        link = default;
        for (var i = 0; i < children.Length; i++)
        {
            if (link.IsEmpty)
            {
                args.HandleInteractionArgs.NotifyChild(
                    children[i],
                    renderPasses[i],
                    this.InnerIdAvailableSlot + i,
                    out link);
            }
            else
            {
                args.HandleInteractionArgs.NotifyChild(
                    children[i],
                    renderPasses[i],
                    this.InnerIdAvailableSlot + i,
                    out _);
            }
        }
    }

    /// <summary>Draw the children.</summary>
    /// <param name="args">The event arguments.</param>
    /// <param name="children">The children in container contents. This may include nulls.</param>
    /// <param name="renderPasses">The render passes for each of the children.</param>
    protected virtual void DrawChildren(
        ControlDrawArgs args,
        ReadOnlySpan<ISpannable> children,
        ReadOnlySpan<ISpannableRenderPass> renderPasses)
    {
        for (var i = 0; i < children.Length; i++)
            args.DrawArgs.NotifyChild(children[i], renderPasses[i]);
    }

    /// <summary>Raises the <see cref="ScrollChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangedEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnScrollChanged(PropertyChangedEventArgs<ControlSpannable, Vector2> args) =>
        this.ScrollChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ScrollBoundaryChanged"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangedEventArgs{TSender,T}"/> that contains the event data.</param>
    protected virtual void OnScrollBoundaryChanged(PropertyChangedEventArgs<ControlSpannable, RectVector4> args) =>
        this.ScrollBoundaryChanged?.Invoke(args);

    /// <summary>Raises the <see cref="ChildAdd"/> event.</summary>
    /// <param name="args">A <see cref="ControlChildArgs"/> that contains the event data.</param>
    protected virtual void OnChildAdd(ControlChildArgs args) => this.ChildAdd?.Invoke(args);

    /// <summary>Raises the <see cref="ChildChange"/> event.</summary>
    /// <param name="args">A <see cref="ControlChildArgs"/> that contains the event data.</param>
    protected virtual void OnChildChange(ControlChildArgs args) => this.ChildChange?.Invoke(args);

    /// <summary>Raises the <see cref="ChildRemove"/> event.</summary>
    /// <param name="args">A <see cref="ControlChildArgs"/> that contains the event data.</param>
    protected virtual void OnChildRemove(ControlChildArgs args) => this.ChildRemove?.Invoke(args);
}

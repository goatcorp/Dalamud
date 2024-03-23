using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Utility.Enumeration;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern spannable that has multiple layers.</summary>
public sealed class LayeredPattern
    : PatternSpannable, IList<ISpannable?>, IReadOnlyList<ISpannable?>, ICollection
{
    /// <inheritdoc cref="ICollection.Count"/>
    public int Count => this.AllChildren.Count - this.AllChildrenAvailableSlot;

    /// <inheritdoc/>
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc/>
    object ICollection.SyncRoot => this;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc cref="IList{T}.this"/>
    public ISpannable? this[int index]
    {
        get
        {
            if (index < 0 || index >= this.AllChildren.Count - this.AllChildrenAvailableSlot)
                throw new IndexOutOfRangeException();
            return this.AllChildren[this.AllChildrenAvailableSlot + index];
        }

        set
        {
            if (index < 0 || index >= this.AllChildren.Count - this.AllChildrenAvailableSlot)
                throw new IndexOutOfRangeException();
            this.AllChildren[this.AllChildrenAvailableSlot + index] = value;
        }
    }

    /// <inheritdoc/>
    public void Add(ISpannable? item) => this.AllChildren.Add(item);

    /// <inheritdoc/>
    public void Clear() =>
        this.AllChildren.RemoveRange(
            this.AllChildrenAvailableSlot,
            this.AllChildren.Count - this.AllChildrenAvailableSlot);

    /// <inheritdoc/>
    public bool Contains(ISpannable? item) => this.AllChildren.IndexOf(item) >= this.AllChildrenAvailableSlot;

    /// <inheritdoc/>
    public void CopyTo(ISpannable?[] array, int arrayIndex) =>
        CollectionsMarshal.AsSpan(this.AllChildren)[this.AllChildrenAvailableSlot..].CopyTo(array.AsSpan(arrayIndex));

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index) => this.CopyTo((ISpannable?[])array, index);

    /// <inheritdoc/>
    public int IndexOf(ISpannable? item)
    {
        var i = this.AllChildren.IndexOf(item);
        if (i >= this.AllChildrenAvailableSlot)
            return i - this.AllChildrenAvailableSlot;
        return -1;
    }

    /// <inheritdoc/>
    public void Insert(int index, ISpannable? item)
    {
        if (index < 0 || index > this.AllChildren.Count - this.AllChildrenAvailableSlot)
            throw new IndexOutOfRangeException();
        this.AllChildren.Insert(this.AllChildrenAvailableSlot + index, item);
    }

    /// <inheritdoc/>
    public bool Remove(ISpannable? item)
    {
        var i = this.AllChildren.IndexOf(item);
        if (i < this.AllChildrenAvailableSlot)
            return false;
        this.AllChildren.RemoveAt(i);
        return true;
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= this.AllChildren.Count - this.AllChildrenAvailableSlot)
            throw new IndexOutOfRangeException();
        this.AllChildren.RemoveAt(this.AllChildrenAvailableSlot + index);
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public ListRangeEnumerator<ISpannable> GetEnumerator() => new(this.AllChildren, this.AllChildrenAvailableSlot..);

    /// <inheritdoc/>
    IEnumerator<ISpannable?> IEnumerable<ISpannable?>.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <inheritdoc/>
    protected override PatternRenderPass CreateNewRenderPass() => new LayeredRenderPass(this);

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class LayeredRenderPass : PatternRenderPass
    {
        private readonly LayeredPattern owner;
        private readonly List<ISpannableRenderPass?> passes = new();

        public LayeredRenderPass(LayeredPattern owner) => this.owner = owner;

        public override void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            base.MeasureSpannable(in args);

            while (this.passes.Count < this.owner.Count)
                this.passes.Add(null);
            this.passes.RemoveRange(this.owner.Count, this.passes.Count - this.owner.Count);
            for (var i = 0; i < this.owner.Count; i++)
            {
                if (this.owner[i] is not { } child)
                    continue;

                this.passes[i] ??= child.RentRenderPass(new(this.Renderer));
                args.NotifyChild(child, this.passes[i], args.MaxSize, args.TextState);
            }
        }

        public override void CommitSpannableMeasurement(scoped in SpannableCommitTransformationArgs args)
        {
            base.CommitSpannableMeasurement(in args);
            for (var i = 0; i < this.owner.Count; i++)
            {
                if (this.owner[i] is not { } child || this.passes[i] is not { } pass)
                    continue;

                args.NotifyChild(child, pass, Vector2.Zero, Matrix4x4.Identity);
            }
        }

        public override void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args, out SpannableLinkInteracted link)
        {
            base.HandleSpannableInteraction(in args, out link);
            for (var i = 0; i < this.owner.Count; i++)
            {
                if (this.owner[i] is not { } child || this.passes[i] is not { } pass)
                    continue;

                if (link.IsEmpty)
                    args.NotifyChild(child, pass, this.owner.InnerIdAvailableSlot + i, out link);
                else
                    args.NotifyChild(child, pass, this.owner.InnerIdAvailableSlot + i, out _);
            }
        }

        public override void DrawSpannable(SpannableDrawArgs args)
        {
            base.DrawSpannable(args);
            for (var i = 0; i < this.owner.Count; i++)
            {
                if (this.owner[i] is not { } child || this.passes[i] is not { } pass)
                    continue;

                args.NotifyChild(child, pass);
            }
        }
    }
}

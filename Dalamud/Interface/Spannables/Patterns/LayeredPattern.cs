using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.RenderPassMethodArgs;
using Dalamud.Utility.Enumeration;

namespace Dalamud.Interface.Spannables.Patterns;

/// <summary>A pattern spannable that has multiple layers.</summary>
public sealed class LayeredPattern : PatternSpannable
{
    private readonly ChildrenCollection childrenCollection;

    /// <summary>Initializes a new instance of the <see cref="LayeredPattern"/> class.</summary>
    public LayeredPattern() => this.childrenCollection = new(this);

    /// <summary>Gets the children as an <see cref="IList{T}"/>.</summary>
    public IList<ISpannable> ChildrenList => this.childrenCollection;

    /// <summary>Gets the children as an <see cref="IReadOnlyList{T}"/>.</summary>
    public IReadOnlyList<ISpannable> ChildrenReadOnlyList => (IReadOnlyList<ISpannable>)this.ChildrenList;

    /// <inheritdoc/>
    protected override PatternRenderPass CreateNewRenderPass() => new LayeredRenderPass(this);

    private class ChildrenCollection(LayeredPattern owner)
        : IList<ISpannable?>, IReadOnlyList<ISpannable?>, ICollection
    {
        /// <inheritdoc cref="ICollection.Count"/>
        public int Count => owner.AllChildren.Count - owner.AllChildrenAvailableSlot;

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
                if (index < 0 || index >= owner.AllChildren.Count - owner.AllChildrenAvailableSlot)
                    throw new IndexOutOfRangeException();
                return owner.AllChildren[owner.AllChildrenAvailableSlot + index];
            }

            set
            {
                if (index < 0 || index >= owner.AllChildren.Count - owner.AllChildrenAvailableSlot)
                    throw new IndexOutOfRangeException();
                owner.AllChildren[owner.AllChildrenAvailableSlot + index] = value;
            }
        }

        /// <inheritdoc/>
        public void Add(ISpannable? item) => owner.AllChildren.Add(item);

        /// <inheritdoc/>
        public void Clear() =>
            owner.AllChildren.RemoveRange(
                owner.AllChildrenAvailableSlot,
                owner.AllChildren.Count - owner.AllChildrenAvailableSlot);

        /// <inheritdoc/>
        public bool Contains(ISpannable? item) => owner.AllChildren.IndexOf(item) >= owner.AllChildrenAvailableSlot;

        /// <inheritdoc/>
        public void CopyTo(ISpannable?[] array, int arrayIndex) =>
            CollectionsMarshal.AsSpan(owner.AllChildren)[owner.AllChildrenAvailableSlot..]
                              .CopyTo(array.AsSpan(arrayIndex));

        /// <inheritdoc/>
        void ICollection.CopyTo(Array array, int index) => this.CopyTo((ISpannable?[])array, index);

        /// <inheritdoc/>
        public int IndexOf(ISpannable? item)
        {
            var i = owner.AllChildren.IndexOf(item);
            if (i >= owner.AllChildrenAvailableSlot)
                return i - owner.AllChildrenAvailableSlot;
            return -1;
        }

        /// <inheritdoc/>
        public void Insert(int index, ISpannable? item)
        {
            if (index < 0 || index > owner.AllChildren.Count - owner.AllChildrenAvailableSlot)
                throw new IndexOutOfRangeException();
            owner.AllChildren.Insert(owner.AllChildrenAvailableSlot + index, item);
        }

        /// <inheritdoc/>
        public bool Remove(ISpannable? item)
        {
            var i = owner.AllChildren.IndexOf(item);
            if (i < owner.AllChildrenAvailableSlot)
                return false;
            owner.AllChildren.RemoveAt(i);
            return true;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= owner.AllChildren.Count - owner.AllChildrenAvailableSlot)
                throw new IndexOutOfRangeException();
            owner.AllChildren.RemoveAt(owner.AllChildrenAvailableSlot + index);
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public ListRangeEnumerator<ISpannable> GetEnumerator() =>
            new(owner.AllChildren, owner.AllChildrenAvailableSlot..);

        /// <inheritdoc/>
        IEnumerator<ISpannable?> IEnumerable<ISpannable?>.GetEnumerator() => this.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>A state for <see cref="LayeredPattern"/>.</summary>
    private class LayeredRenderPass(LayeredPattern owner) : PatternRenderPass(owner)
    {
        private readonly ChildrenCollection children = owner.childrenCollection;
        private readonly List<ISpannableRenderPass?> passes = new();

        public override void MeasureSpannable(scoped in SpannableMeasureArgs args)
        {
            base.MeasureSpannable(in args);

            while (this.passes.Count < this.children.Count)
                this.passes.Add(null);
            this.passes.RemoveRange(this.children.Count, this.passes.Count - this.children.Count);
            for (var i = 0; i < this.children.Count; i++)
            {
                if (this.children[i] is not { } child)
                    continue;

                this.passes[i] ??= child.RentRenderPass(this.Renderer);
                args.NotifyChild(
                    this.passes[i],
                    owner.InnerIdAvailableSlot + i,
                    args with
                    {
                        TextState = this.ActiveTextState.Fork(),
                    });
            }
        }

        public override void CommitSpannableMeasurement(scoped in SpannableCommitMeasurementArgs args)
        {
            base.CommitSpannableMeasurement(in args);
            foreach (var pass in this.passes)
            {
                if (pass is not null)
                    args.NotifyChild(pass, args);
            }
        }

        public override void HandleSpannableInteraction(
            scoped in SpannableHandleInteractionArgs args, out SpannableLinkInteracted link)
        {
            base.HandleSpannableInteraction(in args, out link);
            foreach (var pass in this.passes)
            {
                if (pass is null)
                    continue;

                if (link.IsEmpty)
                    args.NotifyChild(pass, args, out link);
                else
                    args.NotifyChild(pass, args, out _);
            }
        }

        protected override void DrawUntransformed(SpannableDrawArgs args)
        {
            base.DrawUntransformed(args);
            for (var i = 0; i < this.children.Count; i++)
            {
                if (this.passes[i] is { } pass)
                    args.NotifyChild(pass, args);
            }
        }
    }
}

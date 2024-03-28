using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Utility.Enumeration;

using ImGuiNET;

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
    protected override PatternSpannableMeasurement CreateNewRenderPass() => new LayeredRenderPass(this, new());

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
    private class LayeredRenderPass(LayeredPattern owner, SpannableMeasurementOptions options)
        : PatternSpannableMeasurement(owner, options)
    {
        private readonly ChildrenCollection children = owner.childrenCollection;
        private readonly List<ISpannableMeasurement?> childMeasurements = new();

        public override bool Measure()
        {
            var changed = base.Measure();

            while (this.childMeasurements.Count < this.children.Count)
                this.childMeasurements.Add(null);
            this.childMeasurements.RemoveRange(this.children.Count, this.childMeasurements.Count - this.children.Count);

            for (var i = 0; i < this.children.Count; i++)
            {
                if (this.children[i] is not { } child)
                    continue;

                var cm = this.childMeasurements[i] ??= child.RentMeasurement(this.Renderer);
                cm.RenderScale = this.RenderScale;
                cm.Options.Size = this.Boundary.Size;
                changed |= cm.Measure();
            }

            return changed;
        }

        public override void UpdateTransformation(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
        {
            base.UpdateTransformation(in local, in ancestral);
            foreach (var cm in this.childMeasurements)
                cm?.UpdateTransformation(Matrix4x4.Identity, this.FullTransformation);
        }

        public override bool HandleInteraction()
        {
            foreach (var cm in this.childMeasurements)
                cm?.HandleInteraction();
            return base.HandleInteraction();
        }

        protected override void DrawUntransformed(ImDrawListPtr drawListPtr)
        {
            base.DrawUntransformed(drawListPtr);
            foreach (var cm in this.childMeasurements)
                cm?.Draw(drawListPtr);
        }
    }
}

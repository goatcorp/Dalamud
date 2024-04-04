using System.Collections;
using System.Collections.Generic;

using Dalamud.Interface.Spannables.EventHandlers;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables;

#pragma warning disable SA1010

/// <summary>Base class for <see cref="Spannable"/>s.</summary>
public abstract partial class Spannable
{
    private readonly List<Spannable> children = [];
    private readonly List<int> childrenZOrder = [];

    /// <summary>Gets the parent spannable.</summary>
    public Spannable? Parent { get; private set; }

    /// <summary>Gets an enumerable view over the children.</summary>
    /// <param name="forward">Whether to enumerate in forward direction.</param>
    /// <returns>An enumerable view.</returns>
    public IEnumerable<Spannable> EnumerateChildren(bool forward) =>
        ChildrenEnumerator.Pool.Get().Initialize(this, forward);

    /// <summary>Adds a child.</summary>
    /// <param name="child">Child to add.</param>
    /// <remarks>Adding a <c>null</c> is a no-op.</remarks>
    protected void AddChild(Spannable? child)
    {
        if (child is null || child.Parent == this)
            return;
        if (child.Parent is not null)
            throw new InvalidOperationException("The child already has a parent.");

        var zo = this.childrenZOrder.BinarySearch(child.ZOrder);
        if (zo < 0)
            zo = ~zo;
        this.children.Insert(zo, child);
        this.childrenZOrder.Insert(zo, child.ZOrder);

        child.Parent = this;
        child.ZOrderChange += this.ChildOnZOrderChange;
        this.RequestMeasure();
    }

    /// <summary>Removes a child.</summary>
    /// <param name="child">Child to remove.</param>
    /// <remarks>Removing a <c>null</c> is a no-op.</remarks>
    protected void RemoveChild(Spannable? child)
    {
        if (child is null || !ReferenceEquals(child.Parent, this))
            return;
        var i = this.children.IndexOf(child);
        if (i == -1)
            throw new ArgumentException("Not a child", nameof(child));
        child.Parent = null;
        child.ZOrderChange -= this.ChildOnZOrderChange;
        this.children.RemoveAt(i);
        this.childrenZOrder.RemoveAt(i);
        this.RequestMeasure();
    }

    /// <summary>Removes all children and disposes them.</summary>
    protected void ClearChildren()
    {
        foreach (var c in this.children)
        {
            c.Parent = null;
            c.Dispose();
        }

        this.children.Clear();
        this.childrenZOrder.Clear();
        this.RequestMeasure();
    }

    /// <summary>Replaces a child.</summary>
    /// <param name="oldChild">Child to remove.</param>
    /// <param name="newChild">Child to add.</param>
    protected void ReplaceChild(Spannable? oldChild, Spannable? newChild)
    {
        this.RemoveChild(oldChild);
        this.AddChild(newChild);
    }

    private void ChildOnZOrderChange(PropertyChangeEventArgs<int> args)
    {
        if (args.State != PropertyChangeState.After)
            return;

        var child = (Spannable)args.Sender;
        var oi = this.children.IndexOf(child);

        var zo = this.childrenZOrder.BinarySearch(args.NewValue);
        if (zo < 0)
            zo = ~zo;

        this.children.RemoveAt(oi);
        this.childrenZOrder.RemoveAt(oi);

        this.children.Insert(zo, child);
        this.childrenZOrder.Insert(zo, child.ZOrder);
    }

    private sealed class ChildrenEnumerator : IEnumerable<Spannable>, IEnumerator<Spannable>, IResettable
    {
        public static readonly ObjectPool<ChildrenEnumerator> Pool =
            new DefaultObjectPool<ChildrenEnumerator>(new DefaultPooledObjectPolicy<ChildrenEnumerator>());

        private Spannable? parent;
        private bool isForward;
        private int index;

        public Spannable Current =>
            this.parent?.children[this.index] ?? throw new ObjectDisposedException(nameof(ChildrenEnumerator));

        object IEnumerator.Current => this.Current;

        public ChildrenEnumerator Initialize(Spannable p, bool forward)
        {
            this.parent = p;
            this.isForward = forward;
            this.index = forward ? -1 : p.children.Count;
            return this;
        }

        public IEnumerator<Spannable> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;

        public bool MoveNext()
        {
            if (this.parent?.children is not { } children)
                return false;
            if (this.isForward)
            {
                if (this.index + 1 >= children.Count)
                    return false;
                this.index++;
            }
            else
            {
                if (this.index == 0)
                    return false;
                this.index--;
            }

            return true;
        }

        public void Reset() => this.index = this.isForward ? -1 : this.parent?.children.Count ?? 0;

        public void Dispose() => Pool.Return(this);

        public bool TryReset()
        {
            this.parent = null;
            return true;
        }
    }
}

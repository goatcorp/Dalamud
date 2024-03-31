using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables.Patterns;

#pragma warning disable SA1010

/// <summary>A pattern spannable that has multiple layers.</summary>
public sealed class LayeredPattern : AbstractPattern.AbstractSpannable<LayeredPattern.LayerOptions>
{
    private readonly List<Spannable?> children = [];

    /// <summary>Initializes a new instance of the <see cref="LayeredPattern"/> class.</summary>
    /// <param name="options">Layer options.</param>
    /// <param name="sourceTemplate">The source template.</param>
    public LayeredPattern(LayerOptions options, ISpannableTemplate? sourceTemplate = null)
        : base(options, sourceTemplate)
    {
        this.children.EnsureCapacity(this.Options.Children.Count);
        foreach (var c in this.Options.Children)
            this.children.Add(c?.CreateSpannable());
        this.Options.Children.CollectionChanged += this.ChildrenOnCollectionChanged;
    }

    /// <inheritdoc />
    public override IReadOnlyList<Spannable?> GetAllChildSpannables() => this.children;

    /// <inheritdoc/>
    protected override void OnMeasure(SpannableEventArgs args)
    {
        base.OnMeasure(args);

        foreach (var child in this.children)
        {
            if (child is null)
                continue;

            child.Options.RenderScale = this.Options.RenderScale;
            child.Options.VisibleSize = child.Options.PreferredSize = this.Boundary.Size;
            child.RenderPassMeasure();
        }
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);
        foreach (var child in this.children)
            child?.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        base.OnDrawInside(args);
        foreach (var child in this.children)
            child?.RenderPassDraw(args.DrawListPtr);
    }

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems?.Count is > 0:
                this.children.InsertRange(
                    e.NewStartingIndex,
                    e.NewItems!.Cast<ISpannableTemplate?>().Select(x => x?.CreateSpannable()));
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems?.Count is > 0 and var count:
                for (var i = e.OldStartingIndex; i < e.OldStartingIndex + count; i++)
                    this.children[i]?.Dispose();
                this.children.RemoveRange(e.NewStartingIndex, count);
                break;

            case NotifyCollectionChangedAction.Replace when e.NewItems?.Count is > 0 and var count:
                for (var i = 0; i < count; i++)
                {
                    this.children[e.OldStartingIndex + i]?.Dispose();
                    this.children[e.OldStartingIndex + i] = (e.NewItems[i] as ISpannableTemplate)?.CreateSpannable();
                }

                break;

            case NotifyCollectionChangedAction.Move when e.OldItems?.Count is > 0 and var count:
                var slice = this.children.Slice(e.OldStartingIndex, count);
                this.children.RemoveRange(e.OldStartingIndex, count);
                this.children.InsertRange(e.NewStartingIndex, slice);
                break;

            case NotifyCollectionChangedAction.Reset:
                foreach (var c in this.children)
                    c?.Dispose();
                this.children.Clear();

                this.children.EnsureCapacity(this.Options.Children.Count);
                foreach (var c in this.Options.Children)
                    this.children.Add(c?.CreateSpannable());
                break;
        }
    }

    /// <summary>Options for <see cref="LayeredPattern"/>.</summary>
    public class LayerOptions : AbstractPattern.PatternOptions
    {
        /// <summary>Gets the list of children.</summary>
        public ObservableCollection<ISpannableTemplate?> Children { get; } = [];

        /// <inheritdoc/>
        public override void CopyFrom(SpannableOptions source)
        {
            this.Children.Clear();

            if (source is LayerOptions lo)
            {
                foreach (var c in lo.Children)
                    this.Children.Add(c);
            }

            base.CopyFrom(source);
        }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.Children.Clear();
            return base.TryReset();
        }
    }

    /// <summary>A spannable that has multiple layers.</summary>
    public class Template(LayerOptions options) : AbstractPattern.AbstractTemplate<LayerOptions>(options)
    {
        /// <inheritdoc/>
        public override Spannable CreateSpannable() => new LayeredPattern(this.Options, this);
    }
}

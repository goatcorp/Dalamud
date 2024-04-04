using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;

using Dalamud.Interface.Spannables.EventHandlers;

namespace Dalamud.Interface.Spannables.Patterns;

#pragma warning disable SA1010

/// <summary>A pattern spannable that has multiple layers.</summary>
public sealed class LayeredPattern : AbstractPattern
{
    /// <summary>Initializes a new instance of the <see cref="LayeredPattern"/> class.</summary>
    public LayeredPattern() => this.Children.CollectionChanged += this.ChildrenOnCollectionChanged;

    /// <summary>Gets the children.</summary>
    public ObservableCollection<Spannable> Children { get; } = [];

    /// <inheritdoc/>
    protected override void OnMeasure(SpannableMeasureEventArgs args)
    {
        base.OnMeasure(args);

        foreach (var child in this.EnumerateChildren(true))
            child.RenderPassMeasure(this.Boundary.Size);
    }

    /// <inheritdoc/>
    protected override void OnPlace(SpannableEventArgs args)
    {
        base.OnPlace(args);
        foreach (var child in this.EnumerateChildren(true))
            child.RenderPassPlace(Matrix4x4.Identity, this.FullTransformation);
    }

    /// <inheritdoc/>
    protected override void OnDrawInside(SpannableDrawEventArgs args)
    {
        base.OnDrawInside(args);
        foreach (var child in this.EnumerateChildren(false))
            child.RenderPassDraw(args.DrawListPtr);
    }

    private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is { } newItems:
                foreach (var item in newItems)
                    this.AddChild((Spannable)item);
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is { } oldItems:
                foreach (var item in oldItems)
                    this.RemoveChild((Spannable)item);
                break;
            case NotifyCollectionChangedAction.Replace when e.NewItems is { } newItems && e.OldItems is { } oldItems:
                for (var i = 0; i < newItems.Count; i++)
                    this.ReplaceChild((Spannable)oldItems[i], (Spannable)newItems[i]);
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Reset:
                this.ClearChildren();
                foreach (var c in this.Children)
                    this.AddChild(c);
                break;
        }
    }
}

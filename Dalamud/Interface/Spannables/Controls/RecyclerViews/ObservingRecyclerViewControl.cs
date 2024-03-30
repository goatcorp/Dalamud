using System.Collections;
using System.Collections.Specialized;

using Dalamud.Interface.Spannables.Controls.EventHandlers;

namespace Dalamud.Interface.Spannables.Controls.RecyclerViews;

/// <summary>A recycler view control, which is a base for list views and grid views.</summary>
/// <typeparam name="TCollection">Type of the collection that is observable.</typeparam>
public class ObservingRecyclerViewControl<TCollection> : RecyclerViewControl
    where TCollection : ICollection, INotifyCollectionChanged
{
    private TCollection? collection;

    /// <summary>Occurs when the property <see cref="Collection"/> has been changed.</summary>
    public event PropertyChangeEventHandler<TCollection?>? CollectionChange;

    /// <summary>Gets or sets the data collection.</summary>
    public TCollection? Collection
    {
        get => this.collection;
        set => this.HandlePropertyChange(nameof(this.Collection), ref this.collection, value, this.OnCollectionChange);
    }

    /// <inheritdoc/>
    protected override ICollection? GetCollection() => this.collection;

    /// <summary>Raises the <see cref="CollectionChange"/> event.</summary>
    /// <param name="args">A <see cref="PropertyChangeEventArgs{T}"/> that contains the event data.</param>
    protected virtual void OnCollectionChange(
        PropertyChangeEventArgs<TCollection?> args)
    {
        this.CollectionChange?.Invoke(args);

        if (args.State is PropertyChangeState.After && this.LayoutManager is not null)
        {
            if (args.PreviousValue is { } prevData)
                prevData.CollectionChanged -= this.LayoutManager.CollectionChanged;

            if (args.NewValue is { } newData)
                newData.CollectionChanged += this.LayoutManager.CollectionChanged;

            this.LayoutManager.NotifyCollectionReset();
        }
    }

    /// <inheritdoc/>
    protected override void OnLayoutManagerChange(PropertyChangeEventArgs<BaseLayoutManager?> args)
    {
        base.OnLayoutManagerChange(args);

        if (args.State is PropertyChangeState.After)
        {
            // Attempt to do this first, so that double attach is avoided.
            if (args.NewValue is not null)
            {
                ((IProtectedLayoutManager)args.NewValue).SetRecyclerView(this);
                if (this.collection is not null)
                    this.collection.CollectionChanged += args.NewValue.CollectionChanged;
                args.NewValue.CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
            }

            if (args.PreviousValue is not null)
            {
                ((IProtectedLayoutManager)args.PreviousValue).SetRecyclerView(null);
                if (this.collection is not null)
                    this.collection.CollectionChanged -= args.PreviousValue.CollectionChanged;
                args.PreviousValue.CollectionChanged(this, new(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}

// public record PopulateListItemEventArgs : SpannableControlEventArgs
// {
//     public int Index { get; set; }
//
//     public ISpannable? Item { get; set; }
// }
//
// public delegate void PopulateListItemEventHandler(PopulateListItemEventArgs e);
//
// /// <summary>A list view control that may have headers.</summary>
// public class ListViewControl : RecyclerViewControl
// {
//     public event PopulateListItemEventHandler? PopulateListItem;
// }
//
// public record PopulateListItemEventArgs<T> : PopulateListItemEventArgs
// {
//     public T Value { get; set; }
// }
//
// public delegate void PopulateListItemEventHandler<T>(PopulateListItemEventArgs<T> e);
//
// /// <summary>A list view control that may have headers that holds the data.</summary>
// /// <typeparam name="T">Type of the backing data.</typeparam>
// public class ListViewControl<T> : ControlSpannable, IList<T>, IReadOnlyList<T>, ICollection<T>, IReadOnlyCollection<T>
// {
//     public event PopulateListItemEventHandler<T>? PopulateListItem;
// }
//
// public record PopulateGridItemEventArgs : SpannableControlEventArgs
// {
//     public int Row { get; set; }
//     
//     public int Column { get; set; }
//
//     public ISpannable? Item { get; set; }
// }
//
// public delegate void PopulateGridItemEventHandler(PopulateGridItemEventArgs e);
//
// /// <summary>A grid view control that may have headers.</summary>
// // TODO: col/row sizes must be provided
// public class GridViewControl : RecyclerViewControl
// {
// }

// using System.Collections.Generic;
// using System.Collections.ObjectModel;
// using System.Collections.Specialized;
//
// using Dalamud.Interface.Spannables.Controls.EventHandlers;
// using Dalamud.Interface.Spannables.RenderPassMethodArgs;
// using Dalamud.Utility.Numerics;
//
// namespace Dalamud.Interface.Spannables.Controls.TODO;
//
// #pragma warning disable SA1600
//
// public abstract class RecyclerViewLayoutManager
// {
//     public abstract RectVector4 MeasureContentBox(SpannableMeasureArgs args);
//     
//     public abstract void CommitMeasurement(ControlCommitMeasurementEventArgs args);
//
//     public abstract void CollectionChanged(NotifyCollectionChangedEventArgs args);
// }
//
// public class RecyclerViewLinearLayoutManager : RecyclerViewLayoutManager
// {
//     public void ScrollTo(int firstItemIndex) => this.ScrollTo(firstItemIndex, 0);
//
//     public void ScrollTo(int firstItemIndex, float delta) => throw new NotImplementedException();
//     
//     public void SmoothScrollTo(int firstItemIndex) => this.ScrollTo(firstItemIndex, 0);
//
//     public void SmoothScrollTo(int firstItemIndex, float delta) => throw new NotImplementedException();
//     
//     public override RectVector4 MeasureContentBox(SpannableMeasureArgs args) => throw new NotImplementedException();
//
//     public override void CommitMeasurement(ControlCommitMeasurementEventArgs args) => throw new NotImplementedException();
//     
//     public override void CollectionChanged(NotifyCollectionChangedEventArgs args) => throw new NotImplementedException();
// }
//
// /// <summary>A recycler view control, which is a base for list views and grid views.</summary>
// /// <typeparam name="TData">Type of list data.</typeparam>
// // TODO: should this control manage header area?
// public class RecyclerViewControl<TData> : ControlSpannable
// {
//     public delegate void NeedDecideSpannableTypeEventDelegate(NeedDecideSpannableTypeArg args);
//
//     public delegate void NeedMoreSpannableEventDelegate(NeedMoreSpannableEventArg args);
//
//     public delegate void NeedPopulateSpannableEventDelegate(NeedPopulateSpannableEventArg args);
//
//     public event NeedDecideSpannableTypeEventDelegate? NeedDecideSpannableType;
//
//     public event NeedMoreSpannableEventDelegate? NeedMoreSpannables;
//
//     public event NeedPopulateSpannableEventDelegate? NeedPopulateSpannable;
//     
//     public ObservableCollection<TData>? Data { get; set; }
//     
//     public int FirstVisibleItemIndex { get; set; }
//
//     public void AddPlaceholder(int spannableType, ISpannable spannable) => throw new NotImplementedException();
//
//     public record NeedDecideSpannableTypeArg : SpannableControlEventArgs
//     {
//         /// <summary>Gets or sets the index of the item that needs to have its spannable type decided.</summary>
//         public int Index { get; set; }
//         
//         /// <summary>Gets or sets the decided spannable type.</summary>
//         /// <remarks>Assign to this property to assign a spannable type.</remarks>
//         public int SpannableType { get; set; }
//     }
//
//     public record NeedMoreSpannableEventArg : SpannableControlEventArgs
//     {
//         /// <summary>Gets or sets the type of the spannable that needs to be populated.</summary>
//         public int SpannableType { get; set; }
//     }
//
//     public record NeedPopulateSpannableEventArg : SpannableControlEventArgs
//     {
//         /// <summary>Gets or sets the index of the item that needs to have its spannable type decided.</summary>
//         public int Index { get; set; }
//         
//         /// <summary>Gets or sets the associated value.</summary>
//         /// <remarks>Writing to this property will not update the underlying storage.</remarks>
//         public TData Value { get; set; }
//         
//         /// <summary>Gets or sets the decided spannable type from <see cref="NeedDecideSpannableType"/>.</summary>
//         public int SpannableType { get; set; }
//         
//         /// <summary>Gets or sets the associated spannable.</summary>
//         public ISpannable Spannable { get; set; }
//     }
// }
//
// // public record PopulateListItemEventArgs : SpannableControlEventArgs
// // {
// //     public int Index { get; set; }
// //
// //     public ISpannable? Item { get; set; }
// // }
// //
// // public delegate void PopulateListItemEventHandler(PopulateListItemEventArgs e);
// //
// // /// <summary>A list view control that may have headers.</summary>
// // public class ListViewControl : RecyclerViewControl
// // {
// //     public event PopulateListItemEventHandler? PopulateListItem;
// // }
// //
// // public record PopulateListItemEventArgs<T> : PopulateListItemEventArgs
// // {
// //     public T Value { get; set; }
// // }
// //
// // public delegate void PopulateListItemEventHandler<T>(PopulateListItemEventArgs<T> e);
// //
// // /// <summary>A list view control that may have headers that holds the data.</summary>
// // /// <typeparam name="T">Type of the backing data.</typeparam>
// // public class ListViewControl<T> : ControlSpannable, IList<T>, IReadOnlyList<T>, ICollection<T>, IReadOnlyCollection<T>
// // {
// //     public event PopulateListItemEventHandler<T>? PopulateListItem;
// // }
// //
// // public record PopulateGridItemEventArgs : SpannableControlEventArgs
// // {
// //     public int Row { get; set; }
// //     
// //     public int Column { get; set; }
// //
// //     public ISpannable? Item { get; set; }
// // }
// //
// // public delegate void PopulateGridItemEventHandler(PopulateGridItemEventArgs e);
// //
// // /// <summary>A grid view control that may have headers.</summary>
// // // TODO: col/row sizes must be provided
// // public class GridViewControl : RecyclerViewControl
// // {
// // }



using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;

using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Utility.Numerics;

namespace Dalamud.Interface.Spannables.Controls.RecyclerViews;

#pragma warning disable SA1010
#pragma warning disable SA1101

/// <summary>A recycler view control, which is a base for list views and grid views.</summary>
public abstract partial class RecyclerViewControl
{
    /// <summary>Private interface for mutating <see cref="BaseLayoutManager"/>.</summary>
    protected interface IProtectedLayoutManager
    {
        /// <summary>Gets the currently attached parent instance of <see cref="RecyclerViewControl"/>.</summary>
        RecyclerViewControl? Parent { get; }

        /// <summary>Gets the index of the first visible item.</summary>
        /// <value><c>-1</c> if no item is visible.</value>
        public int FirstVisibleItem { get; }

        /// <summary>Gets the index of the last visible item.</summary>
        /// <value><c>-1</c> if no item is visible.</value>
        public int LastVisibleItem { get; }

        /// <summary>Sets the active recycler view.</summary>
        /// <param name="rv">The recycler view to attach, or <c>null</c> to detach.</param>
        void SetRecyclerView(RecyclerViewControl? rv);

        /// <inheritdoc cref="ISpannableMeasurement.HandleInteraction"/>
        void HandleInteraction();

        /// <inheritdoc cref="ControlSpannable.MeasureContentBox"/>
        RectVector4 MeasureContentBox(Vector2 suggestedSize);

        /// <inheritdoc cref="ISpannableMeasurement.Draw"/>
        void Draw(ControlDrawEventArgs args);

        /// <inheritdoc cref="ISpannableMeasurement.UpdateTransformation"/>
        void UpdateTransformation();

        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e);

        /// <summary>Notifies that the collection got reset and all items needs to be inspected again.</summary>
        void NotifyCollectionReset();

        /// <summary>Notifies that the collection has new item(s).</summary>
        /// <param name="startIndex">Index of the first item added.</param>
        /// <param name="count">Number of items added.</param>
        void NotifyCollectionInsert(int startIndex, int count);

        /// <summary>Notifies that the collection lost item(s).</summary>
        /// <param name="startIndex">Index of the first item removed.</param>
        /// <param name="count">Number of items removed.</param>
        void NotifyCollectionRemove(int startIndex, int count);

        /// <summary>Notifies that the collection has its item(s) replaced.</summary>
        /// <param name="startIndex">Index of the first item replaced.</param>
        /// <param name="count">Number of items replaced.</param>
        void NotifyCollectionReplace(int startIndex, int count);

        /// <summary>Notifies that the collection has its item(s) moved.</summary>
        /// <param name="oldStartIndex">Previous index of the first item moved.</param>
        /// <param name="newStartIndex">New index of the first item moved.</param>
        /// <param name="count">Number of items moved.</param>
        void NotifyCollectionMove(int oldStartIndex, int newStartIndex, int count);
    }

    /// <summary>Base layout manager.</summary>
    public abstract class BaseLayoutManager : IProtectedLayoutManager
    {
        private readonly Queue<(
                NotifyCollectionChangedAction Action,
                int NewIndex,
                int NewCount,
                int OldIndex,
                int OldCount)>
            changeQueue = new();

        /// <inheritdoc/>
        public RecyclerViewControl? Parent { get; private set; }

        /// <inheritdoc/>
        public int FirstVisibleItem { get; protected set; }

        /// <inheritdoc/>
        public int LastVisibleItem { get; protected set; }

        /// <summary>Gets or sets a value indicating whether the RV can be scrolled.</summary>
        public bool CanScroll { get; protected set; }

        /// <summary>Gets or sets the smooth scroll easing.</summary>
        public Easing ScrollEasing { get; set; } = new OutCubic(TimeSpan.FromMilliseconds(200));

        /// <summary>Gets a value indicating whether any animation is running.</summary>
        public virtual bool IsAnyAnimationRunning => this.ScrollEasing.IsRunning;

        /// <summary>Gets the underlying list of <see cref="Parent"/>.</summary>
        protected ICollection? Collection => this.Parent?.GetCollection();

        /// <inheritdoc/>
        void IProtectedLayoutManager.SetRecyclerView(RecyclerViewControl? rv)
        {
            if (this.Parent is not null && rv is not null)
                throw new InvalidOperationException("This layout manager is already attached.");
            if (this.Parent is not null)
                this.BeforeParentDetach();
            this.Parent = rv;
        }

        /// <inheritdoc/>
        public void HandleInteraction()
        {
            while (this.changeQueue.TryDequeue(out var e))
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        this.OnCollectionInsert(e.NewIndex, e.NewCount);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        this.OnCollectionRemove(e.OldIndex, e.OldCount);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                    {
                        var startIndex = e.OldIndex;
                        var oldItemCount = e.OldCount;
                        var newItemCount = e.NewCount;
                        var replacementCount = Math.Min(oldItemCount, newItemCount);
                        var delta = newItemCount - oldItemCount;
                        if (replacementCount > 0)
                            this.OnCollectionReplace(startIndex, replacementCount);
                        if (delta > 0)
                            this.OnCollectionInsert(startIndex + replacementCount, delta);
                        else if (delta < 0)
                            this.OnCollectionRemove(startIndex + replacementCount, -delta);
                        break;
                    }

                    case NotifyCollectionChangedAction.Move:
                        this.OnCollectionMove(e.OldIndex, e.NewIndex, e.OldCount);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        this.OnCollectionReset();
                        break;

                    default:
                        return;
                }
            }

            this.HandleInteractionChildren();
        }

        /// <inheritdoc/>
        public RectVector4 MeasureContentBox(Vector2 suggestedSize) => this.MeasureChildren(suggestedSize);

        /// <inheritdoc/>
        public void UpdateTransformation() => this.UpdateTransformationChildren();

        /// <inheritdoc/>
        public void Draw(ControlDrawEventArgs args) => this.DrawChildren(args);

        /// <inheritdoc/>
        public void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.Parent is null)
                return;
            this.changeQueue.Enqueue(
                (
                    e.Action,
                    e.NewStartingIndex,
                    e.NewItems?.Count ?? 0,
                    e.OldStartingIndex,
                    e.OldItems?.Count ?? 0));
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <inheritdoc/>
        public void NotifyCollectionReset()
        {
            if (this.Parent is null)
                return;
            this.changeQueue.Enqueue((NotifyCollectionChangedAction.Reset, 0, 0, 0, 0));
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <inheritdoc/>
        public void NotifyCollectionInsert(int startIndex, int count)
        {
            if (this.Parent is null)
                return;
            this.changeQueue.Enqueue((NotifyCollectionChangedAction.Add, startIndex, count, 0, 0));
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <inheritdoc/>
        public void NotifyCollectionRemove(int startIndex, int count)
        {
            if (this.Parent is null)
                return;
            this.changeQueue.Enqueue((NotifyCollectionChangedAction.Remove, 0, 0, startIndex, count));
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <inheritdoc/>
        public void NotifyCollectionReplace(int startIndex, int count)
        {
            if (this.Parent is null)
                return;
            this.changeQueue.Enqueue((NotifyCollectionChangedAction.Replace, startIndex, count, startIndex, count));
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <inheritdoc/>
        public void NotifyCollectionMove(int oldStartIndex, int newStartIndex, int count)
        {
            if (this.Parent is null)
                return;
            this.changeQueue.Enqueue(
                (NotifyCollectionChangedAction.Replace, oldStartIndex, count, newStartIndex, count));
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <summary>Scrolls by given distance.</summary>
        /// <param name="delta">The scroll distance.</param>
        public abstract void ScrollBy(Vector2 delta);

        /// <summary>Scrolls by given distance with an animation.</summary>
        /// <param name="delta">The scroll distance.</param>
        public abstract void SmoothScrollBy(Vector2 delta);

        /// <summary>Finds the corresponding item index in the collection from a spannable.</summary>
        /// <param name="spannable">The spannable to find the item index.</param>
        /// <returns>The found index, or <c>-1</c> if none found.</returns>
        public abstract int FindItemIndexFromSpannable(ISpannable? spannable);

        /// <summary>Finds the corresponding spannable from the item index.</summary>
        /// <param name="index">The item index.</param>
        /// <returns>The corresponding spannable, or <c>null</c> if none was available.</returns>
        public abstract ISpannableMeasurement? FindMeasurementFromItemIndex(int index);

        /// <inheritdoc cref="ISpannableMeasurement.FindChildMeasurementAt"/>
        public abstract ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset);

        /// <summary><see cref="ISpannableMeasurement.FindChildMeasurementAt"/>, but also looks for children closest
        /// to the given point, if the offset does not match any children.</summary>
        /// <param name="screenOffset">The screen offset.</param>
        /// <returns>A children closest to the given screen offset, or <c>null</c> if there are no children.</returns>
        public abstract ISpannableMeasurement? FindClosestChildMeasurementAt(Vector2 screenOffset);

        /// <summary>Enumerates the spannable measurements and its corresponding item indices.</summary>
        /// <returns>The enumerable.</returns>
        public abstract IEnumerable<(int Index, ISpannableMeasurement Measurement)>
            EnumerateItemSpannableMeasurements();

        /// <summary>Called before detaching parent.</summary>
        protected virtual void BeforeParentDetach()
        {
        }

        /// <summary>Handles interactions for children.</summary>
        protected abstract void HandleInteractionChildren();

        /// <summary>Measures childrens to calculate the size of content box of the parent.</summary>
        /// <param name="suggestedSize">The suggested size.</param>
        /// <returns>The measured content box.</returns>
        protected abstract RectVector4 MeasureChildren(Vector2 suggestedSize);

        /// <summary>Updates transformation matrices for the children.</summary>
        protected abstract void UpdateTransformationChildren();

        /// <summary>Draws the children.</summary>
        /// <param name="args">The event arguments.</param>
        protected abstract void DrawChildren(ControlDrawEventArgs args);

        /// <summary>Processes when the collection got reset and all items needs to be inspected again.</summary>
        protected abstract void OnCollectionReset();

        /// <summary>Processes when the collection has new item(s).</summary>
        /// <param name="startIndex">Index of the first item added.</param>
        /// <param name="count">Number of items added.</param>
        protected abstract void OnCollectionInsert(int startIndex, int count);

        /// <summary>Processes when the collection lost item(s).</summary>
        /// <param name="startIndex">Index of the first item removed.</param>
        /// <param name="count">Number of items removed.</param>
        protected abstract void OnCollectionRemove(int startIndex, int count);

        /// <summary>Processes when the collection has its item(s) replaced.</summary>
        /// <param name="startIndex">Index of the first item replaced.</param>
        /// <param name="count">Number of items replaced.</param>
        protected abstract void OnCollectionReplace(int startIndex, int count);

        /// <summary>Processes when the collection has its item(s) moved.</summary>
        /// <param name="oldStartIndex">Previous index of the first item moved.</param>
        /// <param name="newStartIndex">New index of the first item moved.</param>
        /// <param name="count">Number of items moved.</param>
        protected abstract void OnCollectionMove(int oldStartIndex, int newStartIndex, int count);

        /// <summary>Requests parent to measure again.</summary>
        protected void RequestMeasure() => this.Parent?.OnSpannableChange(this.Parent);

        /// <summary>Requests parent to invoke the scroll event.</summary>
        protected void RequestNotifyScroll()
        {
            if (this.Parent is null)
                return;

            var e = SpannableControlEventArgsPool.Rent<SpannableControlEventArgs>();
            e.Sender = this.Parent;
            this.Parent.OnScroll(e);
            SpannableControlEventArgsPool.Return(e);
        }

        /// <summary>Takes a placeholder, creating new ones as necessary.</summary>
        /// <param name="spannableType">Spannable type of the placeholders in need.</param>
        /// <param name="slotIndex">The slot index in <see cref="ControlSpannable.AllSpannables"/>.</param>
        /// <param name="innerId">The inner ID for this placeholder.</param>
        /// <returns>The placeholder available for use, or <c>null</c> if none could be provided.</returns>
        protected ISpannable? TakePlaceholder(int spannableType, out int slotIndex, out int innerId)
        {
            slotIndex = innerId = -1;
            if (this.Parent is null)
                return null;

            _ = this.Parent.placeholders.TryGetValue(spannableType, out var plist);
            if (plist?.Count is not > 0)
            {
                var e = SpannableControlEventArgsPool.Rent<AddMoreSpannablesEventArg>();
                e.Sender = this.Parent;
                e.SpannableType = spannableType;
                this.Parent.OnAddMoreSpannables(e);
                SpannableControlEventArgsPool.Return(e);

                if (!this.Parent.placeholders.TryGetValue(spannableType, out plist) || plist?.Count is not > 0)
                    return null;
            }

            slotIndex = this.Parent.availablePlaceholderSlotIndices[^1];
            this.Parent.availablePlaceholderSlotIndices.RemoveAt(
                this.Parent.availablePlaceholderSlotIndices.Count - 1);

            innerId = this.Parent.availablePlaceholderInnerIdIndices[^1];
            this.Parent.availablePlaceholderInnerIdIndices.RemoveAt(
                this.Parent.availablePlaceholderInnerIdIndices.Count - 1);

            var t = plist[^1];
            plist.RemoveAt(plist.Count - 1);
            this.Parent.AllSpannables[slotIndex] = t;
            this.Parent.OnSpannableChange(this.Parent);
            return t;
        }

        /// <summary>Returns a placeholder that is no longer in use.</summary>
        /// <param name="spannableType">Spannable type of the placeholder.</param>
        /// <param name="placeholder">The placeholder to return. Can be null, in which case nothing will happen.</param>
        /// <param name="slotIndex">The slot index in <see cref="ControlSpannable.AllSpannables"/>.</param>
        /// <param name="innerId">The inner ID for this placeholder.</param>
        protected void ReturnPlaceholder(int spannableType, ISpannable? placeholder, int slotIndex, int innerId)
        {
            if (placeholder is null || this.Parent is null)
                return;

            if (!this.Parent.placeholders.TryGetValue(spannableType, out var plist))
                this.Parent.placeholders.Add(spannableType, plist = []);
            this.Parent.availablePlaceholderSlotIndices.Add(slotIndex);
            this.Parent.availablePlaceholderInnerIdIndices.Add(innerId);
            plist.Add(placeholder);
            this.Parent.AllSpannables[slotIndex] = null;
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <summary>Resolves the type of spannable for the item at the given index.</summary>
        /// <param name="index">Index of the associated item.</param>
        /// <param name="spannableType">Retrieved spannable type.</param>
        /// <param name="decorationType">Retrieved decoration spannable type.</param>
        protected void ResolveSpannableType(int index, out int spannableType, out int decorationType)
        {
            if (this.Parent is null)
            {
                spannableType = decorationType = InvalidSpannableType;
                return;
            }

            var e = SpannableControlEventArgsPool.Rent<DecideSpannableTypeEventArg>();
            e.Sender = this.Parent;
            e.Index = index;
            e.SpannableType = 0;
            e.DecorationType = InvalidSpannableType;
            this.Parent.OnDecideSpannableType(e);
            spannableType = e.SpannableType;
            decorationType = e.DecorationType;
            SpannableControlEventArgsPool.Return(e);
        }

        /// <summary>Populates the given spannable.</summary>
        /// <param name="index">Index of the associated item.</param>
        /// <param name="spannableType">Type of the spannable.</param>
        /// <param name="spannable">Spannable.</param>
        /// <param name="measurement">Spannable measurement.</param>
        protected void PopulateSpannable(
            int index,
            int spannableType,
            ISpannable spannable,
            ISpannableMeasurement measurement)
        {
            if (this.Parent is null)
                return;
            var e = SpannableControlEventArgsPool.Rent<PopulateSpannableEventArg>();
            e.Index = index;
            e.SpannableType = spannableType;
            e.Spannable = spannable;
            e.Measurement = measurement;
            this.Parent.OnPopulateSpannable(e);
            SpannableControlEventArgsPool.Return(e);
            this.Parent.OnSpannableChange(this.Parent);
        }

        /// <summary>Populates the given spannable.</summary>
        /// <param name="spannableType">Type of the spannable.</param>
        /// <param name="spannable">Spannable.</param>
        /// <param name="measurement">Spannable measurement.</param>
        protected void ClearSpannable(
            int spannableType,
            ISpannable spannable,
            ISpannableMeasurement measurement)
        {
            if (this.Parent is null)
                return;
            var e = SpannableControlEventArgsPool.Rent<ClearSpannableEventArg>();
            e.SpannableType = spannableType;
            e.Spannable = spannable;
            e.Measurement = measurement;
            this.Parent.OnClearSpannable(e);
            SpannableControlEventArgsPool.Return(e);
            this.Parent.OnSpannableChange(this.Parent);
        }
    }
}

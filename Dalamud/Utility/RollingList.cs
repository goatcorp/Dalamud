using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility
{
    /// <summary>
    /// A list with limited capacity holding items of type <typeparamref name="T"/>.
    /// Adding further items will result in the list rolling over.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <remarks>
    /// Implemented as a circular list using a <see cref="List{T}"/> internally.
    /// Insertions and Removals are not supported.
    /// Not thread-safe.
    /// </remarks>
    internal class RollingList<T> : IList<T>
    {
        private List<T> items;
        private int size;
        private int firstIndex;

        /// <summary>Initializes a new instance of the <see cref="RollingList{T}"/> class.</summary>
        /// <param name="size"><see cref="RollingList{T}"/> size.</param>
        /// <param name="capacity">Internal <see cref="List{T}"/> initial capacity.</param>
        public RollingList(int size, int capacity)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionIfLessThan(nameof(size), size, 0);
            capacity = Math.Min(capacity, size);
            this.size = size;
            this.items = new List<T>(capacity);
        }

        /// <summary>Initializes a new instance of the <see cref="RollingList{T}"/> class.</summary>
        /// <param name="size"><see cref="RollingList{T}"/> size.</param>
        public RollingList(int size)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionIfLessThan(nameof(size), size, 0);
            this.size = size;
            this.items = new();
        }

        /// <summary>Initializes a new instance of the <see cref="RollingList{T}"/> class.</summary>
        /// <param name="items">Collection where elements are copied from.</param>
        /// <param name="size"><see cref="RollingList{T}"/> size.</param>
        public RollingList(IEnumerable<T> items, int size)
        {
            if (!items.TryGetNonEnumeratedCount(out var capacity)) capacity = 4;
            capacity = Math.Min(capacity, size);
            this.size = size;
            this.items = new List<T>(capacity);
            this.AddRange(items);
        }

        /// <summary>Initializes a new instance of the <see cref="RollingList{T}"/> class.</summary>
        /// <param name="items">Collection where elements are copied from.</param>
        /// <param name="size"><see cref="RollingList{T}"/> size.</param>
        /// <param name="capacity">Internal <see cref="List{T}"/> initial capacity.</param>
        public RollingList(IEnumerable<T> items, int size, int capacity)
        {
            if (items.TryGetNonEnumeratedCount(out var count) && count > capacity) capacity = count;
            capacity = Math.Min(capacity, size);
            this.size = size;
            this.items = new List<T>(capacity);
            this.AddRange(items);
        }

        /// <summary>Gets item count.</summary>
        public int Count => this.items.Count;

        /// <summary>Gets or sets the internal list capacity.</summary>
        public int Capacity
        {
            get => this.items.Capacity;
            set => this.items.Capacity = Math.Min(value, this.size);
        }

        /// <summary>Gets or sets rolling list size.</summary>
        public int Size
        {
            get => this.size;
            set
            {
                if (value == this.size) return;
                if (value > this.size)
                {
                    if (this.firstIndex > 0)
                    {
                        this.items = new List<T>(this);
                        this.firstIndex = 0;
                    }
                }
                else
                {
                    // value < this._size
                    ThrowHelper.ThrowArgumentOutOfRangeExceptionIfLessThan(nameof(value), value, 0);
                    if (value < this.Count)
                    {
                        this.items = new List<T>(this.TakeLast(value));
                        this.firstIndex = 0;
                    }
                }

                this.size = value;
            }
        }

        /// <summary>Gets a value indicating whether the item is read only.</summary>
        public bool IsReadOnly => false;

        /// <summary>Gets or sets an item by index.</summary>
        /// <param name="index">Item index.</param>
        /// <returns>Item at specified index.</returns>
        public T this[int index]
        {
            get
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionIfGreaterThanOrEqual(nameof(index), index, this.Count);
                ThrowHelper.ThrowArgumentOutOfRangeExceptionIfLessThan(nameof(index), index, 0);
                return this.items[this.GetRealIndex(index)];
            }

            set
            {
                ThrowHelper.ThrowArgumentOutOfRangeExceptionIfGreaterThanOrEqual(nameof(index), index, this.Count);
                ThrowHelper.ThrowArgumentOutOfRangeExceptionIfLessThan(nameof(index), index, 0);
                this.items[this.GetRealIndex(index)] = value;
            }
        }

        /// <summary>Adds an item to this <see cref="RollingList{T}"/>.</summary>
        /// <param name="item">Item to add.</param>
        public void Add(T item)
        {
            if (this.size == 0) return;
            if (this.items.Count >= this.size)
            {
                this.items[this.firstIndex] = item;
                this.firstIndex = (this.firstIndex + 1) % this.size;
            }
            else
            {
                if (this.items.Count == this.items.Capacity)
                {
                    // Manual list capacity resize
                    var newCapacity = Math.Max(Math.Min(this.size, this.items.Capacity * 2), this.items.Capacity);
                    this.items.Capacity = newCapacity;
                }

                this.items.Add(item);
            }

            Debug.Assert(this.items.Count <= this.size, "Item count should be less than Size");
        }

        /// <summary>Add items to this <see cref="RollingList{T}"/>.</summary>
        /// <param name="range">Items to add.</param>
        public void AddRange(IEnumerable<T> range)
        {
            if (this.size == 0) return;
            foreach (var item in range) this.Add(item);
        }

        /// <summary>Removes all elements from the <see cref="RollingList{T}"/>.</summary>
        public void Clear()
        {
            this.items.Clear();
            this.firstIndex = 0;
        }

        /// <summary>Find the index of a specific item.</summary>
        /// <param name="item">item to find.</param>
        /// <returns>Index where <paramref name="item"/> is found. -1 if not found.</returns>
        public int IndexOf(T item)
        {
            var index = this.items.IndexOf(item);
            if (index == -1) return -1;
            return this.GetVirtualIndex(index);
        }

        /// <summary>Not supported.</summary>
        [SuppressMessage("Documentation Rules", "SA1611", Justification = "Not supported")]
        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        /// <summary>Not supported.</summary>
        [SuppressMessage("Documentation Rules", "SA1611", Justification = "Not supported")]
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        /// <summary>Find wether an item exists.</summary>
        /// <param name="item">item to find.</param>
        /// <returns>Wether <paramref name="item"/> is found.</returns>
        public bool Contains(T item) => this.items.Contains(item);

        /// <summary>Copies the content of this list into an array.</summary>
        /// <param name="array">Array to copy into.</param>
        /// <param name="arrayIndex"><paramref name="array"/> index to start coping into.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowHelper.ThrowArgumentOutOfRangeExceptionIfLessThan(nameof(arrayIndex), arrayIndex, 0);
            if (array.Length - arrayIndex < this.Count) ThrowHelper.ThrowArgumentException("Not enough space");
            for (var index = 0; index < this.Count; index++)
            {
                array[arrayIndex++] = this[index];
            }
        }

        /// <summary>Not supported.</summary>
        [SuppressMessage("Documentation Rules", "SA1611", Justification = "Not supported")]
        [SuppressMessage("Documentation Rules", "SA1615", Justification = "Not supported")]
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        /// <summary>Gets an enumerator for this <see cref="RollingList{T}"/>.</summary>
        /// <returns><see cref="RollingList{T}"/> enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var index = 0; index < this.items.Count; index++)
            {
                yield return this.items[this.GetRealIndex(index)];
            }
        }

        /// <summary>Gets an enumerator for this <see cref="RollingList{T}"/>.</summary>
        /// <returns><see cref="RollingList{T}"/> enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetRealIndex(int index) => this.size > 0 ? (index + this.firstIndex) % this.size : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetVirtualIndex(int index) => this.size > 0 ? (this.size + index - this.firstIndex) % this.size : 0;
    }
}

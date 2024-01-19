using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
internal static partial class TrueTypeUtils
{
    private delegate int BinarySearchComparer<T>(in T value);

    private static IDisposable CreatePointerSpan<T>(this T[] data, out PointerSpan<T> pointerSpan)
        where T : unmanaged
    {
        var gchandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        pointerSpan = new(gchandle.AddrOfPinnedObject(), data.Length);
        return Disposable.Create(() => gchandle.Free());
    }

    private static int BinarySearch<T>(this IReadOnlyList<T> span, in T value)
        where T : unmanaged, IComparable<T>
    {
        var l = 0;
        var r = span.Count - 1;
        while (l <= r)
        {
            var i = (int)(((uint)r + (uint)l) >> 1);
            var c = value.CompareTo(span[i]);
            switch (c)
            {
                case 0:
                    return i;
                case > 0:
                    l = i + 1;
                    break;
                default:
                    r = i - 1;
                    break;
            }
        }

        return ~l;
    }

    private static int BinarySearch<T>(this IReadOnlyList<T> span, BinarySearchComparer<T> comparer)
        where T : unmanaged
    {
        var l = 0;
        var r = span.Count - 1;
        while (l <= r)
        {
            var i = (int)(((uint)r + (uint)l) >> 1);
            var c = comparer(span[i]);
            switch (c)
            {
                case 0:
                    return i;
                case > 0:
                    l = i + 1;
                    break;
                default:
                    r = i - 1;
                    break;
            }
        }

        return ~l;
    }

    private static short ReadI16Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadInt16BigEndian(ps.Span[offset..]);

    private static int ReadI32Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadInt32BigEndian(ps.Span[offset..]);

    private static long ReadI64Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadInt64BigEndian(ps.Span[offset..]);

    private static ushort ReadU16Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadUInt16BigEndian(ps.Span[offset..]);

    private static uint ReadU32Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadUInt32BigEndian(ps.Span[offset..]);

    private static ulong ReadU64Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadUInt64BigEndian(ps.Span[offset..]);

    private static Half ReadF16Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadHalfBigEndian(ps.Span[offset..]);

    private static float ReadF32Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadSingleBigEndian(ps.Span[offset..]);

    private static double ReadF64Big(this PointerSpan<byte> ps, int offset) =>
        BinaryPrimitives.ReadDoubleBigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out short value) =>
        value = BinaryPrimitives.ReadInt16BigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out int value) =>
        value = BinaryPrimitives.ReadInt32BigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out long value) =>
        value = BinaryPrimitives.ReadInt64BigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out ushort value) =>
        value = BinaryPrimitives.ReadUInt16BigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out uint value) =>
        value = BinaryPrimitives.ReadUInt32BigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out ulong value) =>
        value = BinaryPrimitives.ReadUInt64BigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out Half value) =>
        value = BinaryPrimitives.ReadHalfBigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out float value) =>
        value = BinaryPrimitives.ReadSingleBigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, int offset, out double value) =>
        value = BinaryPrimitives.ReadDoubleBigEndian(ps.Span[offset..]);

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out short value)
    {
        ps.ReadBig(offset, out value);
        offset += 2;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out int value)
    {
        ps.ReadBig(offset, out value);
        offset += 4;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out long value)
    {
        ps.ReadBig(offset, out value);
        offset += 8;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out ushort value)
    {
        ps.ReadBig(offset, out value);
        offset += 2;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out uint value)
    {
        ps.ReadBig(offset, out value);
        offset += 4;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out ulong value)
    {
        ps.ReadBig(offset, out value);
        offset += 8;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out Half value)
    {
        ps.ReadBig(offset, out value);
        offset += 2;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out float value)
    {
        ps.ReadBig(offset, out value);
        offset += 4;
    }

    private static void ReadBig(this PointerSpan<byte> ps, ref int offset, out double value)
    {
        ps.ReadBig(offset, out value);
        offset += 8;
    }

    private static unsafe T ReadEnumBig<T>(this PointerSpan<byte> ps, int offset) where T : unmanaged, Enum
    {
        switch (Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))))
        {
            case 1:
                var b1 = ps.Span[offset];
                return *(T*)&b1;
            case 2:
                var b2 = ps.ReadU16Big(offset);
                return *(T*)&b2;
            case 4:
                var b4 = ps.ReadU32Big(offset);
                return *(T*)&b4;
            case 8:
                var b8 = ps.ReadU64Big(offset);
                return *(T*)&b8;
            default:
                throw new ArgumentException("Enum is not of size 1, 2, 4, or 8.", nameof(T), null);
        }
    }

    private static void ReadBig<T>(this PointerSpan<byte> ps, int offset, out T value) where T : unmanaged, Enum =>
        value = ps.ReadEnumBig<T>(offset);

    private static void ReadBig<T>(this PointerSpan<byte> ps, ref int offset, out T value) where T : unmanaged, Enum
    {
        value = ps.ReadEnumBig<T>(offset);
        offset += Unsafe.SizeOf<T>();
    }

    private readonly unsafe struct PointerSpan<T> : IList<T>, IReadOnlyList<T>, ICollection
        where T : unmanaged
    {
        public readonly T* Pointer;

        public PointerSpan(T* pointer, int count)
        {
            this.Pointer = pointer;
            this.Count = count;
        }

        public PointerSpan(nint pointer, int count)
            : this((T*)pointer, count)
        {
        }

        public Span<T> Span => new(this.Pointer, this.Count);

        public bool IsEmpty => this.Count == 0;

        public int Count { get; }

        public int Length => this.Count;

        public int ByteCount => sizeof(T) * this.Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        bool ICollection<T>.IsReadOnly => false;

        public ref T this[int index] => ref this.Pointer[this.EnsureIndex(index)];

        public PointerSpan<T> this[Range range] => this.Slice(range.GetOffsetAndLength(this.Count));

        T IList<T>.this[int index]
        {
            get => this.Pointer[this.EnsureIndex(index)];
            set => this.Pointer[this.EnsureIndex(index)] = value;
        }

        T IReadOnlyList<T>.this[int index] => this.Pointer[this.EnsureIndex(index)];

        public bool ContainsPointer<T2>(T2* obj) where T2 : unmanaged =>
            (T*)obj >= this.Pointer && (T*)(obj + 1) <= this.Pointer + this.Count;

        public PointerSpan<T> Slice(int offset, int count) => new(this.Pointer + offset, count);

        public PointerSpan<T> Slice((int Offset, int Count) offsetAndCount)
            => this.Slice(offsetAndCount.Offset, offsetAndCount.Count);

        public PointerSpan<T2> As<T2>(int count)
            where T2 : unmanaged =>
            count > this.Count / sizeof(T2)
                ? throw new ArgumentOutOfRangeException(
                      nameof(count),
                      count,
                      $"Wanted {count} items; had {this.Count / sizeof(T2)} items")
                : new((T2*)this.Pointer, count);

        public PointerSpan<T2> As<T2>()
            where T2 : unmanaged =>
            new((T2*)this.Pointer, this.Count / sizeof(T2));

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < this.Count; i++)
                yield return this[i];
        }

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        bool ICollection<T>.Contains(T item)
        {
            for (var i = 0; i < this.Count; i++)
            {
                if (Equals(this.Pointer[i], item))
                    return true;
            }

            return false;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array.Length < this.Count)
                throw new ArgumentException(null, nameof(array));

            if (array.Length < arrayIndex + this.Count)
                throw new ArgumentException(null, nameof(arrayIndex));

            for (var i = 0; i < this.Count; i++)
                array[arrayIndex + i] = this.Pointer[i];
        }

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        int IList<T>.IndexOf(T item)
        {
            for (var i = 0; i < this.Count; i++)
            {
                if (Equals(this.Pointer[i], item))
                    return i;
            }

            return -1;
        }

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array.Length < this.Count)
                throw new ArgumentException(null, nameof(array));

            if (array.Length < arrayIndex + this.Count)
                throw new ArgumentException(null, nameof(arrayIndex));

            for (var i = 0; i < this.Count; i++)
                array.SetValue(this.Pointer[i], arrayIndex + i);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private int EnsureIndex(int index) =>
            index >= 0 && index < this.Count ? index : throw new IndexOutOfRangeException();
    }

    private readonly unsafe struct BigEndianPointerSpan<T>
        : IList<T>, IReadOnlyList<T>, ICollection
        where T : unmanaged
    {
        public readonly T* Pointer;

        private readonly Func<T, T> reverseEndianness;

        public BigEndianPointerSpan(PointerSpan<T> pointerSpan, Func<T, T> reverseEndianness)
        {
            this.reverseEndianness = reverseEndianness;
            this.Pointer = pointerSpan.Pointer;
            this.Count = pointerSpan.Count;
        }

        public int Count { get; }

        public int Length => this.Count;

        public int ByteCount => sizeof(T) * this.Count;

        public bool IsSynchronized => true;

        public object SyncRoot => this;

        public bool IsReadOnly => true;

        public T this[int index]
        {
            get =>
                BitConverter.IsLittleEndian
                    ? this.reverseEndianness(this.Pointer[this.EnsureIndex(index)])
                    : this.Pointer[this.EnsureIndex(index)];
            set => this.Pointer[this.EnsureIndex(index)] =
                       BitConverter.IsLittleEndian
                           ? this.reverseEndianness(value)
                           : value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < this.Count; i++)
                yield return this[i];
        }

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        bool ICollection<T>.Contains(T item) => throw new NotSupportedException();

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array.Length < this.Count)
                throw new ArgumentException(null, nameof(array));

            if (array.Length < arrayIndex + this.Count)
                throw new ArgumentException(null, nameof(arrayIndex));

            for (var i = 0; i < this.Count; i++)
                array[arrayIndex + i] = this[i];
        }

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();

        int IList<T>.IndexOf(T item)
        {
            for (var i = 0; i < this.Count; i++)
            {
                if (Equals(this[i], item))
                    return i;
            }

            return -1;
        }

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array.Length < this.Count)
                throw new ArgumentException(null, nameof(array));

            if (array.Length < arrayIndex + this.Count)
                throw new ArgumentException(null, nameof(arrayIndex));

            for (var i = 0; i < this.Count; i++)
                array.SetValue(this[i], arrayIndex + i);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private int EnsureIndex(int index) =>
            index >= 0 && index < this.Count ? index : throw new IndexOutOfRangeException();
    }
}

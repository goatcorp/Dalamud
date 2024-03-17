using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Utility.Text;

/// <summary>Enumerates a UTF-8 byte sequence by codepoint.</summary>
public ref struct Utf8SpanEnumerator
{
    private readonly ReadOnlySpan<byte> data;

    /// <summary>Initializes a new instance of the <see cref="Utf8SpanEnumerator"/> struct.</summary>
    /// <param name="data">The UTF-8 byte sequence.</param>
    public Utf8SpanEnumerator(ReadOnlySpan<byte> data) => this.data = data;

    /// <inheritdoc cref="IEnumerator.Current"/>
    public Item Current { get; private set; } = default;

    /// <summary>Attempts to peek the next item.</summary>
    /// <param name="nextItem">The retrieved next item.</param>
    /// <returns><c>true</c> if anything is retrieved.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryPeekNext(out Item nextItem)
    {
        var offset = this.Current.Offset + this.Current.Length;
        var subspan = this.data[offset..];

        if (subspan.IsEmpty)
        {
            nextItem = default;
            return false;
        }

        var isBroken = !Utf8Value.TryDecode(subspan, out var value, out var length);
        nextItem = new(isBroken ? subspan[0] : value, offset, length, isBroken);
        return true;
    }

    /// <inheritdoc cref="IEnumerator.MoveNext"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        if (!this.TryPeekNext(out var next))
            return false;
        this.Current = next;
        return true;
    }

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    public Utf8SpanEnumerator GetEnumerator() => new(this.data);

    /// <summary>A part of a UTF-8 sequence containing one codepoint.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public readonly struct Item : IEquatable<Item>
    {
        /// <summary>The codepoint.</summary>
        [FieldOffset(0)]
        public readonly Utf8Value Value;

        /// <summary>The offset of this part of a UTF-8 sequence.</summary>
        [FieldOffset(4)]
        public readonly int Offset;

        /// <summary>The length of this part of a UTF-8 sequence.</summary>
        /// <remarks>This may not match <see cref="Utf8Value.Length"/>, if <see cref="BrokenSequence"/> is <c>true</c>.
        /// </remarks>
        [FieldOffset(8)]
        public readonly int Length;

        /// <summary>Whether this part of the UTF-8 sequence is broken.</summary>
        [FieldOffset(12)]
        public readonly bool BrokenSequence;

        [FieldOffset(0)]
        private readonly ulong storage0;

        [FieldOffset(8)]
        private readonly ulong storage1;

        /// <summary>Initializes a new instance of the <see cref="Item"/> struct.</summary>
        /// <param name="codepoint">The codepoint.</param>
        /// <param name="offset">The offset of this part of a UTF-8 sequence.</param>
        /// <param name="length">The length of this part of a UTF-8 sequence.</param>
        /// <param name="brokenSequence">Whether this part of the UTF-8 sequence is broken.</param>
        public Item(uint codepoint, int offset, int length, bool brokenSequence)
        {
            this.Value = new(codepoint);
            this.Offset = offset;
            this.Length = length;
            this.BrokenSequence = brokenSequence;
        }

        /// <summary>Gets the effective <c>char</c> value, with invalid or non-representable codepoints replaced.
        /// </summary>
        public char EffectiveChar =>
            this.BrokenSequence || !this.Value.TryGetRune(out var rune)
                ? '\uFFFD'
                : rune.IsBmp
                    ? (char)rune.Value
                    : '\u3013';

        public static bool operator ==(Item left, Item right) => left.Equals(right);

        public static bool operator !=(Item left, Item right) => !left.Equals(right);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Item other) => this.storage0 == other.storage0 && this.storage1 == other.storage1;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Item other && this.Equals(other);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(this.storage0, this.storage1);
    }
}

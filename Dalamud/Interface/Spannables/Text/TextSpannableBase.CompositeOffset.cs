using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="TextSpannable"/> and <see cref="TextSpannableBuilder"/>.</summary>
public abstract partial class TextSpannableBase
{
    /// <summary>Represents an offset in a <see cref="TextSpannable"/>.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private protected struct CompositeOffset : IEquatable<CompositeOffset>, IComparable<CompositeOffset>, IComparable
    {
        /// <summary>The offset in the text stream.</summary>
        [FieldOffset(0)]
        public int Text;

        /// <summary>The record index.</summary>
        [FieldOffset(4)]
        public int Record;

        /// <summary>Value for quick equality comparison.</summary>
        [FieldOffset(0)]
        private ulong data;

        /// <summary>Initializes a new instance of the <see cref="CompositeOffset"/> struct.</summary>
        /// <param name="text">The text offset.</param>
        /// <param name="record">The record index.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompositeOffset(int text, int record)
        {
            this.Text = text;
            this.Record = record;
        }

        /// <summary>Initializes a new instance of the <see cref="CompositeOffset"/> struct.</summary>
        /// <param name="segment">The segment to copy indices from.</param>
        /// <param name="textDelta">The delta offset for texts.</param>
        /// <param name="recordDelta">The delta offset for records.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompositeOffset(DataRef.Segment segment, int textDelta = 0, int recordDelta = 0)
            : this(segment.Offset.Text + textDelta, segment.Offset.Record + recordDelta)
        {
        }

        /// <summary>Gets a value indicating that the offset has no value.</summary>
        public static CompositeOffset Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(-1, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CompositeOffset left, CompositeOffset right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CompositeOffset left, CompositeOffset right) => !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(CompositeOffset left, CompositeOffset right) => left.CompareTo(right) < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(CompositeOffset left, CompositeOffset right) => left.CompareTo(right) <= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(CompositeOffset left, CompositeOffset right) => left.CompareTo(right) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(CompositeOffset left, CompositeOffset right) => left.CompareTo(right) >= 0;

        /// <summary>Adds text offset.</summary>
        /// <param name="offset">The offset to add.</param>
        /// <returns>The added offset.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly CompositeOffset AddTextOffset(int offset) => new(this.Text + offset, this.Record);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(CompositeOffset other) =>
            this.Text == other.Text ? this.Record.CompareTo(other.Record) : this.Text.CompareTo(other.Text);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(object? obj) => obj switch
        {
            null => -1,
            CompositeOffset other => this.CompareTo(other),
            _ => throw new ArgumentException(null, nameof(obj)),
        };

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(CompositeOffset other) => this.data == other.data;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object? obj) => obj is CompositeOffset other && this.Equals(other);

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode() => this.data.GetHashCode();
    }
}

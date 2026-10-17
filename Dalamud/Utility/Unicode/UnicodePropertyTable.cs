using System.Runtime.CompilerServices;

namespace Dalamud.Utility.Unicode;

/// <summary>The values of a Unicode property by code point, U+0000 to U+10FFFF.</summary>
/// <typeparam name="T">The enum of the property's values.</typeparam>
/// <remarks>Two-stage lookup table with a block-index array mapping each code point block to a deduplicated block of
/// property values.</remarks>
public readonly ref struct UnicodePropertyTable<T> where T : unmanaged, Enum
{
    /// <summary>Base-2 logarithm of the block size (128 code points per block), read by
    /// <c>Dalamud.UnicodeDataGenerator</c> when generating <see cref="UnicodeData"/> tables.</summary>
    internal const int BlockBits = 7;

    private readonly ReadOnlySpan<byte> blocks;
    private readonly ReadOnlySpan<byte> values;

    /// <summary>Initializes a new instance of the <see cref="UnicodePropertyTable{T}"/> struct.</summary>
    /// <param name="blocks">The index of each block of code points among the distinct blocks.</param>
    /// <param name="values">The distinct blocks' values.</param>
    internal UnicodePropertyTable(ReadOnlySpan<byte> blocks, ReadOnlySpan<byte> values)
    {
        this.blocks = blocks;
        this.values = values;
    }

    /// <summary>Gets the total number of code points covered by the table (<c>0x110000</c>).</summary>
    public int Length => (this.blocks.Length / 2) << BlockBits;

    /// <summary>Gets the value of a code point.</summary>
    /// <param name="codepoint">The code point, from 0 to 0x10FFFF.</param>
    /// <exception cref="IndexOutOfRangeException">The code point is out of range.</exception>
    public T this[int codepoint]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            const int mask = (1 << BlockBits) - 1;
            var blockIndex = (codepoint >> BlockBits) * 2;

            var block = this.blocks[blockIndex] | (this.blocks[blockIndex + 1] << 8);
            return Unsafe.BitCast<byte, T>(this.values[(block << BlockBits) | (codepoint & mask)]);
        }
    }
}

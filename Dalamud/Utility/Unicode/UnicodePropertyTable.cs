using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Dalamud.Utility.Unicode;

/// <summary>The values of a Unicode property by code point, U+0000 to U+10FFFF.</summary>
/// <typeparam name="T">The enum of the property's values.</typeparam>
/// <remarks>A two-stage table. It holds the index of each block of code points among the distinct blocks, and the
/// distinct blocks' values one after another.</remarks>
public readonly ref struct UnicodePropertyTable<T> where T : unmanaged, Enum
{
    /// <summary>The number of code points of a block, as a power of two. Dalamud.UnicodeDataGenerator makes the tables
    /// of <see cref="UnicodeData"/>'s properties with blocks of this size.</summary>
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

    /// <summary>Gets the number of code points, all of them.</summary>
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
            var block = BinaryPrimitives.ReadUInt16LittleEndian(this.blocks[((codepoint >> BlockBits) * 2)..]);
            return Unsafe.BitCast<byte, T>(this.values[(block << BlockBits) | (codepoint & mask)]);
        }
    }
}

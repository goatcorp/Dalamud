using System.Collections.Generic;
using System.IO;

namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Reference member view of a .fdt file data.
/// </summary>
internal readonly unsafe struct FdtFileView
{
    private readonly byte* ptr;

    /// <summary>
    /// Initializes a new instance of the <see cref="FdtFileView"/> struct.
    /// </summary>
    /// <param name="ptr">Pointer to the data.</param>
    /// <param name="length">Length of the data.</param>
    public FdtFileView(void* ptr, int length)
    {
        this.ptr = (byte*)ptr;
        if (length < sizeof(FdtReader.FdtHeader))
            throw new InvalidDataException("Not enough space for a FdtHeader");

        if (length < this.FileHeader.FontTableHeaderOffset + sizeof(FdtReader.FontTableHeader))
            throw new InvalidDataException("Not enough space for a FontTableHeader");
        if (length < this.FileHeader.FontTableHeaderOffset + sizeof(FdtReader.FontTableHeader) +
            (sizeof(FdtReader.FontTableEntry) * this.FontHeader.FontTableEntryCount))
            throw new InvalidDataException("Not enough space for all the FontTableEntry");

        if (length < this.FileHeader.KerningTableHeaderOffset + sizeof(FdtReader.KerningTableHeader))
            throw new InvalidDataException("Not enough space for a KerningTableHeader");
        if (length < this.FileHeader.KerningTableHeaderOffset + sizeof(FdtReader.KerningTableHeader) +
            (sizeof(FdtReader.KerningTableEntry) * this.KerningEntryCount))
            throw new InvalidDataException("Not enough space for all the KerningTableEntry");
    }

    /// <summary>
    /// Gets the file header.
    /// </summary>
    public ref FdtReader.FdtHeader FileHeader => ref *(FdtReader.FdtHeader*)this.ptr;

    /// <summary>
    /// Gets the font header.
    /// </summary>
    public ref FdtReader.FontTableHeader FontHeader =>
        ref *(FdtReader.FontTableHeader*)((nint)this.ptr + this.FileHeader.FontTableHeaderOffset);

    /// <summary>
    /// Gets the glyphs.
    /// </summary>
    public Span<FdtReader.FontTableEntry> Glyphs => new(this.GlyphsUnsafe, this.FontHeader.FontTableEntryCount);

    /// <summary>
    /// Gets the kerning header.
    /// </summary>
    public ref FdtReader.KerningTableHeader KerningHeader =>
        ref *(FdtReader.KerningTableHeader*)((nint)this.ptr + this.FileHeader.KerningTableHeaderOffset);

    /// <summary>
    /// Gets the number of kerning entries.
    /// </summary>
    public int KerningEntryCount => Math.Min(this.FontHeader.KerningTableEntryCount, this.KerningHeader.Count);

    /// <summary>
    /// Gets the kerning entries.
    /// </summary>
    public Span<FdtReader.KerningTableEntry> PairAdjustments => new(
        this.ptr + this.FileHeader.KerningTableHeaderOffset + sizeof(FdtReader.KerningTableHeader),
        this.KerningEntryCount);

    /// <summary>
    /// Gets the maximum texture index.
    /// </summary>
    public int MaxTextureIndex
    {
        get
        {
            var i = 0;
            foreach (ref var g in this.Glyphs)
            {
                if (g.TextureIndex > i)
                    i = g.TextureIndex;
            }

            return i;
        }
    }

    private FdtReader.FontTableEntry* GlyphsUnsafe =>
        (FdtReader.FontTableEntry*)(this.ptr + this.FileHeader.FontTableHeaderOffset +
                                    sizeof(FdtReader.FontTableHeader));

    /// <summary>
    /// Finds the glyph index for the corresponding codepoint.
    /// </summary>
    /// <param name="codepoint">Unicode codepoint (UTF-32 value).</param>
    /// <returns>Corresponding index, or a negative number according to <see cref="List{T}.BinarySearch(int,int,T,System.Collections.Generic.IComparer{T}?)"/>.</returns>
    public int FindGlyphIndex(int codepoint)
    {
        var comp = FdtReader.CodePointToUtf8Int32(codepoint);

        var glyphs = this.GlyphsUnsafe;
        var lo = 0;
        var hi = this.FontHeader.FontTableEntryCount - 1;
        while (lo <= hi)
        {
            var i = (int)(((uint)hi + (uint)lo) >> 1);
            switch (comp.CompareTo(glyphs[i].CharUtf8))
            {
                case 0:
                    return i;
                case > 0:
                    lo = i + 1;
                    break;
                default:
                    hi = i - 1;
                    break;
            }
        }

        return ~lo;
    }

    /// <summary>
    /// Create a glyph range for use with <see cref="Interface.ManagedFontAtlas.SafeFontConfig.GlyphRanges"/>.
    /// </summary>
    /// <param name="mergeDistance">Merge two ranges into one if distance is below the value specified in this parameter.</param>
    /// <returns>Glyph ranges.</returns>
    public ushort[] ToGlyphRanges(int mergeDistance = 8)
    {
        var glyphs = this.Glyphs;
        var ranges = new List<ushort>(glyphs.Length)
        {
            checked((ushort)glyphs[0].CharInt),
            checked((ushort)glyphs[0].CharInt),
        };
        
        foreach (ref var glyph in glyphs[1..])
        {
            var c32 = glyph.CharInt;
            if (c32 >= 0x10000)
                break;

            var c16 = unchecked((ushort)c32);
            if (ranges[^1] + mergeDistance >= c16 && c16 > ranges[^1])
            {
                ranges[^1] = c16;
            }
            else if (ranges[^1] + 1 < c16)
            {
                ranges.Add(c16);
                ranges.Add(c16);
            }
        }

        ranges.Add(0);
        return ranges.ToArray();
    }
}

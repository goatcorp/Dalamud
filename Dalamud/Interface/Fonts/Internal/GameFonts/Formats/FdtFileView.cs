using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.Fonts.Internal.GameFonts.Formats;

/// <summary>View of a game font file (.fdt).</summary>
internal readonly struct FdtFileView
{
    private readonly ReadOnlyMemory<byte> data;
    private readonly int glyphTableHeaderOffset;
    private readonly int kerningPairTableHeaderOffset;
    private readonly int glyphsOffset;
    private readonly int kerningPairsOffset;
    private readonly int safeGlyphCount;
    private readonly int safeKerningPairCount;

    /// <summary>Initializes a new instance of the <see cref="FdtFileView"/> struct.</summary>
    /// <param name="data">Contents of the file.</param>
    /// <exception cref="InvalidDataException">If a header does not fit in the data or a signature is wrong.
    /// </exception>
    /// <remarks>Tables that run past the end of the data are cut short.</remarks>
    public FdtFileView(ReadOnlyMemory<byte> data)
    {
        this.data = data;
        try
        {
            ref readonly var fileHeader = ref MemoryMarshal.AsRef<FdtFileHeader>(data.Span);
            this.glyphTableHeaderOffset = fileHeader.GlyphTableHeaderOffset;
            this.kerningPairTableHeaderOffset = fileHeader.KerningTableHeaderOffset;
            if (!fileHeader.ValidateSignature()
                || !this.GlyphTableHeader.ValidateSignature()
                || !this.KerningPairTableHeader.ValidateSignature())
                throw new InvalidDataException("Invalid signature");
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            throw new InvalidDataException("Truncated file", e);
        }

        this.glyphsOffset = this.glyphTableHeaderOffset + Unsafe.SizeOf<FdtGlyphTableHeader>();
        this.kerningPairsOffset = this.kerningPairTableHeaderOffset + Unsafe.SizeOf<FdtKerningPairTableHeader>();
        this.safeGlyphCount = Math.Clamp(
            this.GlyphTableHeader.GlyphCount,
            0,
            (data.Length - this.glyphsOffset) / Unsafe.SizeOf<FdtGlyph>());
        this.safeKerningPairCount = Math.Clamp(
            Math.Min(this.GlyphTableHeader.KerningPairCount, this.KerningPairTableHeader.Count),
            0,
            (data.Length - this.kerningPairsOffset) / Unsafe.SizeOf<FdtKerningPair>());
    }

    /// <summary>Gets the header of the glyph table, which also holds the font metrics.</summary>
    public ref readonly FdtGlyphTableHeader GlyphTableHeader =>
        ref MemoryMarshal.AsRef<FdtGlyphTableHeader>(this.data.Span[this.glyphTableHeaderOffset..]);

    /// <summary>Gets the glyphs, sorted by character.</summary>
    public ReadOnlySpan<FdtGlyph> Glyphs =>
        MemoryMarshal.Cast<byte, FdtGlyph>(
            this.data.Span.Slice(this.glyphsOffset, this.safeGlyphCount * Unsafe.SizeOf<FdtGlyph>()));

    /// <summary>Gets the header of the kerning pair table.</summary>
    public ref readonly FdtKerningPairTableHeader KerningPairTableHeader =>
        ref MemoryMarshal.AsRef<FdtKerningPairTableHeader>(this.data.Span[this.kerningPairTableHeaderOffset..]);

    /// <summary>Gets the kerning pairs, sorted by left then right character.</summary>
    public ReadOnlySpan<FdtKerningPair> KerningPairs =>
        MemoryMarshal.Cast<byte, FdtKerningPair>(
            this.data.Span.Slice(this.kerningPairsOffset, this.safeKerningPairCount * Unsafe.SizeOf<FdtKerningPair>()));

    /// <summary>Gets the number of texture files the glyphs use, at least 1.</summary>
    public int TextureFileCount
    {
        get
        {
            var i = 0;
            foreach (ref readonly var g in this.Glyphs)
                i = Math.Max(i, g.TextureFileIndex);
            return i + 1;
        }
    }

    /// <summary>Finds the glyph of a character.</summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <param name="glyph">The glyph, if found.</param>
    /// <returns>Whether the font has the character.</returns>
    public bool TryGetGlyph(FdtRune codepoint, out FdtGlyph glyph)
    {
        var glyphs = this.Glyphs;
        var i = glyphs.BinarySearch(codepoint);
        glyph = i >= 0 ? glyphs[i] : default;
        return i >= 0;
    }

    /// <summary>Gets the kerning between two characters.</summary>
    /// <param name="left">The codepoint of the left character.</param>
    /// <param name="right">The codepoint of the right character.</param>
    /// <returns>The offset of the right character in pixels, or 0 if the pair has none.</returns>
    public int GetKerning(FdtRune left, FdtRune right)
    {
        var pairs = this.KerningPairs;
        var i = pairs.BinarySearch(new PairKey(left, right));
        return i >= 0 ? pairs[i].RightOffset : 0;
    }

    private readonly struct PairKey(FdtRune left, FdtRune right) : IComparable<FdtKerningPair>
    {
        public int CompareTo(FdtKerningPair other) =>
            left == other.Left ? right.CompareTo(other.Right) : left.CompareTo(other.Left);
    }
}

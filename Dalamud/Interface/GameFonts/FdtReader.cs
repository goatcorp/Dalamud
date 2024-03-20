using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Parses a game font file.
/// </summary>
public class FdtReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FdtReader"/> class.
    /// </summary>
    /// <param name="data">Content of a FDT file.</param>
    public FdtReader(byte[] data)
    {
        this.FileHeader = StructureFromByteArray<FdtHeader>(data, 0);
        this.FontHeader = StructureFromByteArray<FontTableHeader>(data, this.FileHeader.FontTableHeaderOffset);
        this.KerningHeader = StructureFromByteArray<KerningTableHeader>(data, this.FileHeader.KerningTableHeaderOffset);

        for (var i = 0; i < this.FontHeader.FontTableEntryCount; i++)
            this.Glyphs.Add(StructureFromByteArray<FontTableEntry>(data, this.FileHeader.FontTableHeaderOffset + Marshal.SizeOf<FontTableHeader>() + (Marshal.SizeOf<FontTableEntry>() * i)));

        for (int i = 0, to = Math.Min(this.FontHeader.KerningTableEntryCount, this.KerningHeader.Count); i < to; i++)
            this.Distances.Add(StructureFromByteArray<KerningTableEntry>(data, this.FileHeader.KerningTableHeaderOffset + Marshal.SizeOf<KerningTableHeader>() + (Marshal.SizeOf<KerningTableEntry>() * i)));
    }

    /// <summary>
    /// Gets the header of this file.
    /// </summary>
    public FdtHeader FileHeader { get; init; }

    /// <summary>
    /// Gets the font header of this file.
    /// </summary>
    public FontTableHeader FontHeader { get; init; }

    /// <summary>
    /// Gets the kerning table header of this file.
    /// </summary>
    public KerningTableHeader KerningHeader { get; init; }

    /// <summary>
    /// Gets all the glyphs defined in this file.
    /// </summary>
    public List<FontTableEntry> Glyphs { get; init; } = new();

    /// <summary>
    /// Gets all the kerning entries defined in this file.
    /// </summary>
    public List<KerningTableEntry> Distances { get; init; } = new();

    /// <summary>
    /// Finds the glyph index for the corresponding codepoint.
    /// </summary>
    /// <param name="codepoint">Unicode codepoint (UTF-32 value).</param>
    /// <returns>Corresponding index, or a negative number according to <see cref="List{T}.BinarySearch(int,int,T,System.Collections.Generic.IComparer{T}?)"/>.</returns>
    public int FindGlyphIndex(int codepoint) =>
        this.Glyphs.BinarySearch(new FontTableEntry { CharUtf8 = CodePointToUtf8Int32(codepoint) });

    /// <summary>
    /// Finds glyph definition for corresponding codepoint.
    /// </summary>
    /// <param name="codepoint">Unicode codepoint (UTF-32 value).</param>
    /// <returns>Corresponding FontTableEntry, or null if not found.</returns>
    public FontTableEntry? FindGlyph(int codepoint)
    {
        var i = this.FindGlyphIndex(codepoint);
        if (i < 0 || i == this.Glyphs.Count)
            return null;
        return this.Glyphs[i];
    }

    /// <summary>
    /// Returns glyph definition for corresponding codepoint.
    /// </summary>
    /// <param name="codepoint">Unicode codepoint (UTF-32 value).</param>
    /// <returns>Corresponding FontTableEntry, or that of a fallback character.</returns>
    public FontTableEntry GetGlyph(int codepoint)
    {
        return (this.FindGlyph(codepoint)
                ?? this.FindGlyph('〓')
                ?? this.FindGlyph('?')
                ?? this.FindGlyph('='))!.Value;
    }

    /// <summary>
    /// Returns distance adjustment between two adjacent characters.
    /// </summary>
    /// <param name="codepoint1">Left character.</param>
    /// <param name="codepoint2">Right character.</param>
    /// <returns>Supposed distance adjustment between given characters.</returns>
    public int GetDistance(int codepoint1, int codepoint2)
    {
        var i = this.Distances.BinarySearch(new KerningTableEntry { LeftUtf8 = CodePointToUtf8Int32(codepoint1), RightUtf8 = CodePointToUtf8Int32(codepoint2) });
        if (i < 0 || i == this.Distances.Count)
            return 0;
        return this.Distances[i].RightOffset;
    }

    /// <summary>
    /// Translates a UTF-32 codepoint to a <see cref="uint"/> containing a UTF-8 character.
    /// </summary>
    /// <param name="codepoint">The codepoint.</param>
    /// <returns>The uint.</returns>
    internal static int CodePointToUtf8Int32(int codepoint)
    {
        if (codepoint <= 0x7F)
        {
            return codepoint;
        }
        else if (codepoint <= 0x7FF)
        {
            return ((0xC0 | (codepoint >> 6)) << 8)
                   | ((0x80 | ((codepoint >> 0) & 0x3F)) << 0);
        }
        else if (codepoint <= 0xFFFF)
        {
            return ((0xE0 | (codepoint >> 12)) << 16)
                   | ((0x80 | ((codepoint >> 6) & 0x3F)) << 8)
                   | ((0x80 | ((codepoint >> 0) & 0x3F)) << 0);
        }
        else if (codepoint <= 0x10FFFF)
        {
            return ((0xF0 | (codepoint >> 18)) << 24)
                   | ((0x80 | ((codepoint >> 12) & 0x3F)) << 16)
                   | ((0x80 | ((codepoint >> 6) & 0x3F)) << 8)
                   | ((0x80 | ((codepoint >> 0) & 0x3F)) << 0);
        }
        else
        {
            return 0xFFFE;
        }
    }

    private static unsafe T StructureFromByteArray<T>(byte[] data, int offset)
    {
        var len = Marshal.SizeOf<T>();
        if (offset + len > data.Length)
            throw new Exception("Data too short");

        fixed (byte* ptr = data)
            return Marshal.PtrToStructure<T>(new(ptr + offset));
    }

    private static int Utf8Uint32ToCodePoint(int n)
    {
        if ((n & 0xFFFFFF80) == 0)
        {
            return n & 0x7F;
        }
        else if ((n & 0xFFFFE0C0) == 0xC080)
        {
            return
                (((n >> 0x08) & 0x1F) << 6) |
                (((n >> 0x00) & 0x3F) << 0);
        }
        else if ((n & 0xF0C0C0) == 0xE08080)
        {
            return
                (((n >> 0x10) & 0x0F) << 12) |
                (((n >> 0x08) & 0x3F) << 6) |
                (((n >> 0x00) & 0x3F) << 0);
        }
        else if ((n & 0xF8C0C0C0) == 0xF0808080)
        {
            return
                (((n >> 0x18) & 0x07) << 18) |
                (((n >> 0x10) & 0x3F) << 12) |
                (((n >> 0x08) & 0x3F) << 6) |
                (((n >> 0x00) & 0x3F) << 0);
        }
        else
        {
            return 0xFFFF; // Guaranteed non-unicode
        }
    }

    /// <summary>
    /// Header of game font file format.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FdtHeader
    {
        /// <summary>
        /// Signature: "fcsv".
        /// </summary>
        public fixed byte Signature[8];

        /// <summary>
        /// Offset to FontTableHeader.
        /// </summary>
        public int FontTableHeaderOffset;

        /// <summary>
        /// Offset to KerningTableHeader.
        /// </summary>
        public int KerningTableHeaderOffset;

        /// <summary>
        /// Unused/unknown.
        /// </summary>
        public fixed byte Padding[0x10];
    }

    /// <summary>
    /// Header of glyph table.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FontTableHeader
    {
        /// <summary>
        /// Signature: "fthd".
        /// </summary>
        public fixed byte Signature[4];

        /// <summary>
        /// Number of glyphs defined in this file.
        /// </summary>
        public int FontTableEntryCount;

        /// <summary>
        /// Number of kerning informations defined in this file.
        /// </summary>
        public int KerningTableEntryCount;

        /// <summary>
        /// Unused/unknown.
        /// </summary>
        public fixed byte Padding[0x04];

        /// <summary>
        /// Width of backing texture.
        /// </summary>
        public ushort TextureWidth;

        /// <summary>
        /// Height of backing texture.
        /// </summary>
        public ushort TextureHeight;

        /// <summary>
        /// Size of the font defined from this file, in points unit.
        /// </summary>
        public float Size;

        /// <summary>
        /// Line height of the font defined forom this file, in pixels unit.
        /// </summary>
        public int LineHeight;

        /// <summary>
        /// Ascent of the font defined from this file, in pixels unit.
        /// </summary>
        public int Ascent;

        /// <summary>
        /// Gets descent of the font defined from this file, in pixels unit.
        /// </summary>
        public int Descent => this.LineHeight - this.Ascent;
    }

    /// <summary>
    /// Glyph table entry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FontTableEntry : IComparable<FontTableEntry>
    {
        /// <summary>
        /// Mapping of texture channel index to byte index.
        /// </summary>
        public static readonly int[] TextureChannelOrder = { 2, 1, 0, 3 };

        /// <summary>
        /// Integer representation of a Unicode character in UTF-8 in reverse order, read in little endian.
        /// </summary>
        public int CharUtf8;

        /// <summary>
        /// Integer representation of a Shift_JIS character in reverse order, read in little endian.
        /// </summary>
        public ushort CharSjis;

        /// <summary>
        /// Index of backing texture.
        /// </summary>
        public ushort TextureIndex;

        /// <summary>
        /// Horizontal offset of glyph image in the backing texture.
        /// </summary>
        public ushort TextureOffsetX;

        /// <summary>
        /// Vertical offset of glyph image in the backing texture.
        /// </summary>
        public ushort TextureOffsetY;

        /// <summary>
        /// Bounding width of this glyph.
        /// </summary>
        public byte BoundingWidth;

        /// <summary>
        /// Bounding height of this glyph.
        /// </summary>
        public byte BoundingHeight;

        /// <summary>
        /// Distance adjustment for drawing next character.
        /// </summary>
        public sbyte NextOffsetX;

        /// <summary>
        /// Distance adjustment for drawing current character.
        /// </summary>
        public sbyte CurrentOffsetY;

        /// <summary>
        /// Gets the index of the file among all the backing texture files.
        /// </summary>
        public int TextureFileIndex => this.TextureIndex / 4;

        /// <summary>
        /// Gets the channel index in the backing texture file.
        /// </summary>
        public int TextureChannelIndex => this.TextureIndex % 4;

        /// <summary>
        /// Gets the byte index in a multichannel pixel corresponding to the channel.
        /// </summary>
        public int TextureChannelByteIndex => TextureChannelOrder[this.TextureChannelIndex];

        /// <summary>
        /// Gets the advance width of this character.
        /// </summary>
        public int AdvanceWidth => this.BoundingWidth + this.NextOffsetX;

        /// <summary>
        /// Gets the Unicode codepoint of the character for this entry in int type.
        /// </summary>
        public int CharInt => Utf8Uint32ToCodePoint(this.CharUtf8);

        /// <summary>
        /// Gets the Unicode codepoint of the character for this entry in char type.
        /// </summary>
        public char Char => (char)Utf8Uint32ToCodePoint(this.CharUtf8);

        /// <inheritdoc/>
        public int CompareTo(FontTableEntry other)
        {
            return this.CharUtf8 - other.CharUtf8;
        }
    }

    /// <summary>
    /// Header of kerning table.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct KerningTableHeader
    {
        /// <summary>
        /// Signature: "knhd".
        /// </summary>
        public fixed byte Signature[4];

        /// <summary>
        /// Number of kerning entries in this table.
        /// </summary>
        public int Count;

        /// <summary>
        /// Unused/unknown.
        /// </summary>
        public fixed byte Padding[0x08];
    }

    /// <summary>
    /// Kerning table entry.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KerningTableEntry : IComparable<KerningTableEntry>
    {
        /// <summary>
        /// Integer representation of a Unicode character in UTF-8 in reverse order, read in little endian, for the left character.
        /// </summary>
        public int LeftUtf8;

        /// <summary>
        /// Integer representation of a Unicode character in UTF-8 in reverse order, read in little endian, for the right character.
        /// </summary>
        public int RightUtf8;

        /// <summary>
        /// Integer representation of a Shift_JIS character in reverse order, read in little endian, for the left character.
        /// </summary>
        public ushort LeftSjis;

        /// <summary>
        /// Integer representation of a Shift_JIS character in reverse order, read in little endian, for the right character.
        /// </summary>
        public ushort RightSjis;

        /// <summary>
        /// Horizontal offset adjustment for the right character.
        /// </summary>
        public int RightOffset;

        /// <summary>
        /// Gets the Unicode codepoint of the character for this entry in int type.
        /// </summary>
        public int LeftInt => Utf8Uint32ToCodePoint(this.LeftUtf8);

        /// <summary>
        /// Gets the Unicode codepoint of the character for this entry in char type.
        /// </summary>
        public char Left => (char)Utf8Uint32ToCodePoint(this.LeftUtf8);

        /// <summary>
        /// Gets the Unicode codepoint of the character for this entry in int type.
        /// </summary>
        public int RightInt => Utf8Uint32ToCodePoint(this.RightUtf8);

        /// <summary>
        /// Gets the Unicode codepoint of the character for this entry in char type.
        /// </summary>
        public char Right => (char)Utf8Uint32ToCodePoint(this.RightUtf8);

        /// <inheritdoc/>
        public int CompareTo(KerningTableEntry other)
        {
            if (this.LeftUtf8 == other.LeftUtf8)
                return this.RightUtf8 - other.RightUtf8;
            else
                return this.LeftUtf8 - other.LeftUtf8;
        }
    }
}

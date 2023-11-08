#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Unicode;

using ImGuiNET;

namespace Dalamud.CorePlugin.MyFonts.ImFontWrappers;

internal unsafe class ScaledImFontWrapper : ImFontWrapper
{
    public ScaledImFontWrapper(FontChainAtlas atlas, ImFontWrapper src, float scale)
        : base(atlas, (BitArray)src.LoadAttemptedGlyphs.Clone())
    {
        this.IndexedHotData.AddRange(src.IndexedHotData);
        this.FrequentKerningPairs.AddRange(src.FrequentKerningPairs);
        this.IndexLookup.AddRange(src.IndexLookup);
        this.Glyphs.AddRange(src.Glyphs);
        this.KerningPairs.AddRange(src.KerningPairs);
        this.Font.FontSize = src.Font.FontSize * scale;
        this.Font.FallbackChar = src.Font.FallbackChar;
        this.Font.EllipsisChar = src.Font.EllipsisChar;
        this.Font.DotChar = src.Font.DotChar;
        this.Font.DirtyLookupTables = src.Font.DirtyLookupTables;
        this.Font.Scale = src.Font.Scale;
        this.Font.Ascent = src.Font.Ascent * scale;
        this.Font.Descent = src.Font.Descent * scale;
        this.Font.FallbackGlyph = (ImFontGlyph*)this.FindLoadedGlyphNoFallback(this.Font.FallbackChar);
        this.Font.FallbackHotData = (ImFontGlyphHotData*)(this.IndexedHotData.Data + this.Font.FallbackChar);

        foreach (var c in Enumerable.Range(0, this.IndexLookup.Length))
        {
            var glyphIndex = this.IndexLookup[c];
            if (glyphIndex != ushort.MaxValue)
                this.Glyphs[glyphIndex].XY *= scale;
        }

        this.RepairHotData();
    }

    /// <inheritdoc/>
    public override bool IsCharAvailable(char c) => this.FindLoadedGlyphNoFallback(c) != null;

    /// <inheritdoc/>
    public override void LoadGlyphs(IEnumerable<char> chars)
    {
    }

    /// <inheritdoc/>
    public override void LoadGlyphs(IEnumerable<UnicodeRange> ranges)
    {
    }
}

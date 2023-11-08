#nullable enable
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Unicode;

using ImGuiNET;

namespace Dalamud.CorePlugin.MyFonts.OnDemandFonts;

internal unsafe class ScaledOnDemandFont : OnDemandFont
{
    public ScaledOnDemandFont(OnDemandAtlas atlas, OnDemandFont src, float scale)
        : base(atlas, (BitArray)src.LoadAttemptedGlyphs.Clone())
    {
        this.IndexedHotData.AddRange(src.IndexedHotData);
        this.FrequentKerningPairs.Clear();
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
        this.Font.FallbackHotData =
            this.Font.FallbackChar == ushort.MaxValue
                ? null
                : (ImFontGlyphHotData*)(this.IndexedHotData.Data + this.Font.FallbackChar);

        foreach (ref var glyph in this.Glyphs.AsSpan)
        {
            glyph.XY *= scale;
            glyph.AdvanceX = MathF.Round(glyph.AdvanceX * scale);
        }

        foreach (ref var hd in this.IndexedHotData.AsSpan)
        {
            hd.AdvanceX = MathF.Round(hd.AdvanceX * scale);
            hd.OccupiedWidth = MathF.Ceiling(hd.OccupiedWidth * scale);
        }

        foreach (ref var k in this.KerningPairs.AsSpan)
        {
            if (k is not { Left: < FrequentKerningPairsMaxCodepoint, Right: < FrequentKerningPairsMaxCodepoint })
                continue;

            ref var d = ref this.FrequentKerningPairs[(k.Left * FrequentKerningPairsMaxCodepoint) + k.Right];
            d = MathF.Round(d * scale);
        }

        this.UpdateReferencesToVectorItems();
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
